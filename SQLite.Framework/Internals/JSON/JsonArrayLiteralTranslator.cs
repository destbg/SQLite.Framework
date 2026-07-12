namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Translates inline array and list literals, such as <c>new[] { x, 6 }</c> or
/// <c>new List&lt;int&gt; { x }</c>, into a <c>json_array(...)</c> SQL expression so they can
/// act as JSON collections inside a query. Every element is written in the same form the
/// registered JSON serializer uses inside a stored list.
/// </summary>
internal static class JsonArrayLiteralTranslator
{
    public static SQLiteExpression? TryTranslate(SQLVisitor visitor, Expression body)
    {
        if (body is NewArrayExpression { NodeType: ExpressionType.NewArrayBounds } bounds)
        {
            return TranslateBounds(visitor, bounds);
        }

        IReadOnlyList<Expression>? elements = body switch
        {
            NewArrayExpression { NodeType: ExpressionType.NewArrayInit } newArray => newArray.Expressions,
            ListInitExpression listInit when listInit.Initializers.All(i => i.Arguments.Count == 1)
                => listInit.Initializers.Select(i => i.Arguments[0]).ToList(),
            _ => null
        };

        if (elements == null)
        {
            return null;
        }

        if (elements.Count == 0)
        {
            return SQLiteExpression.Leaf(body.Type, visitor.Counters.NextIdentifier(), "json_array()", (SQLiteParameter[]?)null).WithJsonSource();
        }

        SQLiteExpression[] parts = new SQLiteExpression[elements.Count];
        List<SQLiteParameter> combined = [];
        for (int i = 0; i < elements.Count; i++)
        {
            SQLiteExpression? element = TryTranslate(visitor, elements[i]) ?? TranslateElement(visitor, elements[i]);
            if (element == null)
            {
                return null;
            }

            parts[i] = element;
            if (element.Parameters != null)
            {
                combined.AddRange(element.Parameters);
            }
        }

        return SQLiteExpression
            .Variadic(body.Type, visitor.Counters.NextIdentifier(), "json_array(", parts, ", ", ")", combined.Count > 0 ? [.. combined] : null)
            .WithJsonSource();
    }

    private static SQLiteExpression TranslateBounds(SQLVisitor visitor, NewArrayExpression bounds)
    {
        if (bounds.Expressions.Count != 1)
        {
            throw new NotSupportedException(
                "A multi-dimensional array is not supported inside a JSON collection query. Use a one-dimensional array.");
        }

        if (!ExpressionHelpers.IsConstant(bounds.Expressions[0]))
        {
            throw new NotSupportedException(
                "A new array with a computed length is not supported inside a JSON collection query. Use a constant or captured length.");
        }

        int length = Convert.ToInt32(ExpressionHelpers.GetConstantValue(bounds.Expressions[0]), CultureInfo.InvariantCulture);
        if (length <= 0)
        {
            return SQLiteExpression.Leaf(bounds.Type, visitor.Counters.NextIdentifier(), "json_array()", (SQLiteParameter[]?)null).WithJsonSource();
        }

        Type elementType = bounds.Type.GetElementType()!;
        string? defaultSql = DefaultElementSql(elementType);
        if (defaultSql == null)
        {
            throw new NotSupportedException(
                $"A new array of '{elementType.Name}' filled with default elements is not supported inside a JSON collection query. List the elements explicitly instead.");
        }

        string sql = "json_array(" + string.Join(", ", Enumerable.Repeat(defaultSql, length)) + ")";
        return SQLiteExpression.Leaf(bounds.Type, visitor.Counters.NextIdentifier(), sql, (SQLiteParameter[]?)null).WithJsonSource();
    }

    private static string? DefaultElementSql(Type elementType)
    {
        if (Nullable.GetUnderlyingType(elementType) != null || !elementType.IsValueType)
        {
            return "NULL";
        }

        if (elementType == typeof(bool))
        {
            return "json('false')";
        }

        if (elementType == typeof(int)
            || elementType == typeof(long)
            || elementType == typeof(short)
            || elementType == typeof(byte)
            || elementType == typeof(sbyte)
            || elementType == typeof(uint)
            || elementType == typeof(ulong)
            || elementType == typeof(ushort)
            || elementType == typeof(double)
            || elementType == typeof(float)
            || elementType == typeof(decimal))
        {
            return "0";
        }

        return null;
    }

    private static SQLiteExpression? TranslateElement(SQLVisitor visitor, Expression element)
    {
        SQLiteOptions options = visitor.Database.Options;
        Type elementType = Nullable.GetUnderlyingType(element.Type) ?? element.Type;
        ResolvedModel resolved = visitor.ResolveExpression(element);
        if (resolved.SQLiteExpression == null)
        {
            return null;
        }

        if (JsonMethodTranslator.TryGetComparableConstant(resolved, out object? value))
        {
            SQLiteExpression? constantElement = TranslateConstantElement(visitor, options, elementType, value);
            if (constantElement != null)
            {
                return constantElement;
            }
        }

        SQLiteExpression sql = resolved.SQLiteExpression;

        if (elementType == typeof(bool))
        {
            string boolSql = sql.ToString();
            return SQLiteExpression.Leaf(element.Type, visitor.Counters.NextIdentifier(),
                $"(CASE WHEN ({boolSql}) IS NULL THEN NULL WHEN ({boolSql}) THEN json('true') ELSE json('false') END)",
                sql.Parameters);
        }

        if (!TypeHelpers.IsSimple(elementType, options) || options.HasJsonConverter(elementType))
        {
            return SQLiteExpression.Wrap(element.Type, visitor.Counters.NextIdentifier(), "json(", sql, ")", sql.Parameters);
        }

        if (elementType == typeof(decimal) && options.DecimalStorage == DecimalStorageMode.Text)
        {
            return SQLiteExpression.Wrap(element.Type, visitor.Counters.NextIdentifier(), "json(", sql, ")", sql.Parameters);
        }

        return sql;
    }

    private static SQLiteExpression? TranslateConstantElement(SQLVisitor visitor, SQLiteOptions options, Type elementType, object? value)
    {
        if (JsonTemporalText.TryFormat(value, out string? temporalText))
        {
            return SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), temporalText);
        }

        if (value is Enum)
        {
            if (JsonEnumText.TryFormat(options, value, out string? enumText))
            {
                return SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), enumText);
            }

            if (options.EnumStorage == EnumStorageMode.Text)
            {
                return SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), CommandHelpers.EnumToInt64(value));
            }
        }

        if (value is char charValue && options.CharStorage == CharStorageMode.Integer)
        {
            return SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), charValue.ToString());
        }

        if (value != null && !TypeHelpers.IsSimple(elementType, options))
        {
            throw new NotSupportedException(
                $"A captured value of type '{elementType.Name}' inside an inline array needs a registered JSON converter for that type so it can be written as a JSON element.");
        }

        return null;
    }
}
