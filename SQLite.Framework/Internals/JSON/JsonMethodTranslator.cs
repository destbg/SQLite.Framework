namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Dispatches LINQ collection-method calls (<see cref="Enumerable" />, <see cref="List{T}" />,
/// <see cref="Array" />) to JSON-aware SQL translations when the source is a JSON-stored column.
/// Returns <see langword="null" /> when the call is not on a JSON-stored source so callers can
/// fall through to the regular dispatch.
/// </summary>
internal static class JsonMethodTranslator
{
    private static readonly HashSet<string> ArrayLambdaMethodNames =
    [
        nameof(Array.Exists),
        nameof(Array.Find),
        nameof(Array.FindAll),
        nameof(Array.FindIndex),
        nameof(Array.FindLast),
        nameof(Array.FindLastIndex),
        nameof(Array.TrueForAll),
        nameof(Array.ConvertAll),
    ];

    public static Expression? TryHandle(MethodCallExpression node, SQLVisitor visitor)
    {
        Type declaring = node.Method.DeclaringType!;

        Expression? sourceExpr = node.Object ?? (node.Arguments.Count > 0 ? node.Arguments[0] : null);
        bool sourceIsEnumerableChain = sourceExpr is MethodCallExpression mce
            && mce.Method.DeclaringType == typeof(Enumerable);
        if (!sourceIsEnumerableChain
            && (sourceExpr == null || !IsJsonCollection(sourceExpr.Type, visitor.Database.Options)))
        {
            return null;
        }

        if (declaring == typeof(Enumerable))
        {
            return JsonCollectionVisitor.TryHandle(node, visitor) ?? TryEnumerable(node, visitor);
        }

        if (declaring.IsGenericType && declaring.GetGenericTypeDefinition() == typeof(List<>))
        {
            return TryList(node, visitor);
        }

        if (declaring == typeof(Array))
        {
            return TryArray(node, visitor);
        }

        return null;
    }

    private static SQLiteExpression? TryEnumerable(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        if (source.SQLiteExpression == null || !IsJsonCollection(source.SQLiteExpression.Type, visitor.Database.Options))
        {
            return null;
        }

        string src = source.SQLiteExpression.Sql;
        SQLiteParameter[]? parameters = source.SQLiteExpression.Parameters;

        if (node.Arguments.Count == 1)
        {
            string? sql = node.Method.Name switch
            {
                nameof(Enumerable.Any) => $"json_array_length({src}) > 0",
                nameof(Enumerable.Count) => $"json_array_length({src})",
                nameof(Enumerable.First) or nameof(Enumerable.FirstOrDefault) => $"json_extract({src}, '$[0]')",
                nameof(Enumerable.Last) or nameof(Enumerable.LastOrDefault) =>
                    $"CASE WHEN json_array_length({src}) > 0 THEN json_extract({src}, '$[' || (json_array_length({src}) - 1) || ']') ELSE NULL END",
                nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault) =>
                    $"CASE WHEN json_array_length({src}) = 1 THEN json_extract({src}, '$[0]') ELSE NULL END",
                nameof(Enumerable.Min) => $"(SELECT MIN(value) FROM json_each({src}))",
                nameof(Enumerable.Max) => $"(SELECT MAX(value) FROM json_each({src}))",
                nameof(Enumerable.Sum) => $"(SELECT SUM(value) FROM json_each({src}))",
                nameof(Enumerable.Average) => $"(SELECT AVG(value) FROM json_each({src}))",
                nameof(Enumerable.Distinct) => $"(SELECT json_group_array(DISTINCT value) FROM json_each({src}))",
                nameof(Enumerable.Reverse) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) ORDER BY key DESC))",
                _ => null,
            };

            return sql == null ? null : new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, sql, parameters) { IsJsonSource = true };
        }

        if (node.Arguments.Count == 2)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[1]);
            string argSql = arg.Sql!;
            SQLiteParameter[]? combined = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);

            string? sql = node.Method.Name switch
            {
                nameof(Enumerable.Concat) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) UNION ALL SELECT value FROM json_each({argSql})))",
                nameof(Enumerable.Union) => $"(SELECT json_group_array(value) FROM (SELECT DISTINCT value FROM json_each({src}) UNION SELECT DISTINCT value FROM json_each({argSql})))",
                nameof(Enumerable.Intersect) => $"(SELECT json_group_array(value) FROM json_each({src}) WHERE value IN (SELECT value FROM json_each({argSql})))",
                nameof(Enumerable.Except) => $"(SELECT json_group_array(value) FROM json_each({src}) WHERE value NOT IN (SELECT value FROM json_each({argSql})))",
                _ => null,
            };

            if (sql != null)
            {
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, sql, combined) { IsJsonSource = true };
            }
        }

        return null;
    }

    private static SQLiteExpression? TryList(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Object!);
        string src = source.SQLiteExpression!.Sql;

        if (node.Method.Name == nameof(List<>.Contains) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++,
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE value = {arg.Sql})",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.IndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.Sql} LIMIT 1), -1)",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.LastIndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.Sql} ORDER BY key DESC LIMIT 1), -1)",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.GetRange) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.Sql} OFFSET {idx.Sql}))",
                CombineAll(source.SQLiteExpression, idx.SQLiteExpression, cnt.SQLiteExpression))
            { IsJsonSource = true };
        }

#if NET9_0_OR_GREATER
        if (node.Method.Name == nameof(List<>.Slice) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.Sql} OFFSET {idx.Sql}))",
                CombineAll(source.SQLiteExpression, idx.SQLiteExpression, cnt.SQLiteExpression))
            { IsJsonSource = true };
        }
#endif

        return TryListPredicate(node, visitor, source.SQLiteExpression, src);
    }

    private static SQLiteExpression? TryListPredicate(MethodCallExpression node, SQLVisitor visitor, SQLiteExpression source, string src)
    {
        if (node.Arguments.Count != 1)
        {
            return null;
        }

        if (node.Method.Name is not (
            nameof(List<>.Exists)
            or nameof(List<>.Find)
            or nameof(List<>.FindAll)
            or nameof(List<>.FindIndex)
            or nameof(List<>.FindLast)
            or nameof(List<>.FindLastIndex)
            or nameof(List<>.TrueForAll)))
        {
            return null;
        }

        Expression stripped = ExpressionHelpers.StripQuotes(node.Arguments[0]);
        if (stripped is not LambdaExpression lambda)
        {
            return null;
        }

        (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
        SQLiteParameter[]? combined = CombineAll(source, predParams == null ? null : new SQLiteExpression(typeof(object), -1, "", predParams) { IsJsonSource = true });

        return node.Method.Name switch
        {
            nameof(List<>.Exists) => new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++,
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
            { IsJsonSource = true },
            nameof(List<>.Find) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindAll) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
            { IsJsonSource = true },
            nameof(List<>.FindIndex) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindLast) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindLastIndex) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
            { IsJsonSource = true },
            _ => new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++,
                $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
            { IsJsonSource = true },
        };
    }

    private static SQLiteExpression? TryArray(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        string src = source.SQLiteExpression!.Sql;

        if (node.Arguments.Count == 2 && node.Arguments[1] is Expression secondArg)
        {
            Expression stripped = ExpressionHelpers.StripQuotes(secondArg);
            if (stripped is LambdaExpression lambda && ArrayLambdaMethodNames.Contains(node.Method.Name))
            {
                (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
                SQLiteParameter[]? combined = CombineAll(source.SQLiteExpression, predParams == null ? null : new SQLiteExpression(typeof(object), -1, "", predParams) { IsJsonSource = true });
                return node.Method.Name switch
                {
                    nameof(Array.Exists) => new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++,
                        $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
                    { IsJsonSource = true },
                    nameof(Array.Find) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindAll) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                        $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindIndex) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindLast) => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindLastIndex) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.TrueForAll) => new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++,
                        $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
                    { IsJsonSource = true },
                    _ => new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++,
                        $"(SELECT json_group_array({predSql}) FROM json_each({src}))", combined)
                    { IsJsonSource = true },
                };
            }

            ResolvedModel arg = visitor.ResolveExpression(secondArg);
            string argSql = arg.Sql!;
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);

            return node.Method.Name switch
            {
                nameof(Array.IndexOf) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} LIMIT 1), -1)", parameters)
                { IsJsonSource = true },
                nameof(Array.LastIndexOf) => new SQLiteExpression(typeof(int), visitor.Counters.IdentifierIndex++,
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} ORDER BY key DESC LIMIT 1), -1)", parameters)
                { IsJsonSource = true },
                _ => null,
            };
        }

        return null;
    }

    private static (string Sql, SQLiteParameter[]? Parameters) VisitElementLambda(SQLVisitor visitor, LambdaExpression lambda)
    {
        ParameterExpression param = lambda.Parameters[0];
        Type elementType = param.Type;

        Dictionary<string, Expression> bindings;
        if (TypeHelpers.IsSimple(elementType, visitor.Database.Options))
        {
            SQLiteExpression valueExpr = new(elementType, -1, "value", (SQLiteParameter[]?)null);
            bindings = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            bindings = new Dictionary<string, Expression>();
            RegisterProperties(elementType, "value", bindings);
        }

        visitor.MethodArguments[param] = bindings;
        try
        {
            Expression result = visitor.Visit(lambda.Body);
            if (result is SQLiteExpression sqlExpr)
            {
                return (sqlExpr.Sql, sqlExpr.Parameters);
            }

            throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
        }
        finally
        {
            visitor.MethodArguments.Remove(param);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private static void RegisterProperties(Type type, string valueSql, Dictionary<string, Expression> dict)
    {
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string sql = $"json_extract({valueSql}, '$.{prop.Name}')";
            dict[prop.Name] = new SQLiteExpression(prop.PropertyType, -1, sql, (SQLiteParameter[]?)null) { IsJsonSource = true };
        }
    }

    private static bool IsJsonCollection(Type type, SQLiteOptions options)
    {
        return options.TypeConverters.ContainsKey(type)
               && TypeHelpers.GetEnumerableElementType(type) != null;
    }

    private static SQLiteParameter[]? CombineAll(params SQLiteExpression?[] exprs)
    {
        SQLiteExpression[] non = exprs.Where(e => e != null).Cast<SQLiteExpression>().ToArray();
        return ParameterHelpers.CombineParameters(non);
    }
}
