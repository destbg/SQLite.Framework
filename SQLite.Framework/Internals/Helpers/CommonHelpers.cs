namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// A home for small helper operations that each used to live in their own single-method class.
/// Grouped here to keep the helper folder from filling up with one-method files.
/// </summary>
internal static class CommonHelpers
{
    /// <summary>
    /// Inlines captured <see cref="Queryable{T}" /> wrappers into the LINQ expression tree before translation.
    /// </summary>
    public static Expression Inline(Expression node)
    {
        return new CapturedQueryableInlinerVisitor().Visit(node);
    }

    /// <summary>
    /// Resolves the JSON property name for a member, honoring a <see cref="JsonPropertyNameAttribute" />
    /// so a renamed property matches the key written by the serializer.
    /// </summary>
    public static string JsonMemberName(MemberInfo member)
    {
        return member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
    }

    /// <summary>
    /// Reports whether the hooks for <typeparamref name="T" /> include a column-collecting hook,
    /// which writes extra columns into a dictionary during a write.
    /// </summary>
    public static bool HasColumnHooks<T>(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks)
    {
        return hooks.TryGetValue(typeof(T), out IReadOnlyList<Delegate>? list)
            && list.Any(h => h is Func<SQLiteDatabase, T, IDictionary<string, object?>, bool>);
    }

    /// <summary>
    /// Reports whether a lambda body references its first parameter.
    /// </summary>
    public static bool Uses(LambdaExpression lambda)
    {
        ParameterUsageFinderVisitor finder = new(lambda.Parameters[0]);
        finder.Visit(lambda.Body);
        return finder.Found;
    }

    /// <summary>
    /// Expands table-row references passed as method-call arguments into member-init expressions.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Result is used as an expression tree, never compiled.")]
    public static LambdaExpression ExpandRowsInMethodCalls(LambdaExpression lambda, IEnumerable<ParameterExpression> rowParameters)
    {
        HashSet<ParameterExpression> set = [.. rowParameters];

        if (set.Count == 0)
        {
            return lambda;
        }

        RowParameterExpanderVisitor expander = new(set);
        Expression body = expander.Visit(lambda.Body);
        return body == lambda.Body ? lambda : Expression.Lambda(body, lambda.Parameters);
    }

    /// <summary>
    /// Rebinds a filter lambda from an interface or base type onto a concrete entity type.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Entity types are preserved by the user Table<T>().")]
    public static LambdaExpression Rebind(LambdaExpression source, Type entityType)
    {
        ParameterExpression oldP = source.Parameters[0];
        if (oldP.Type == entityType)
        {
            return source;
        }

        ParameterExpression newP = Expression.Parameter(entityType, oldP.Name);
        QueryFilterRebinderVisitor visitor = new(oldP, newP);
        Expression body = visitor.Visit(source.Body)!;
        Type funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        return Expression.Lambda(funcType, body, newP);
    }

    /// <summary>
    /// Resolves the database column name from a <c>Set</c> target expression. The target is either a
    /// property on the entity or <see cref="SQLiteColumn.Of{TValue}" /> for a column with no CLR
    /// property. Shared by the migration and write-column builders.
    /// </summary>
    public static string Resolve<T, TValue>(TableMapping mapping, Expression<Func<T, TValue>> column)
    {
        Expression body = column.Body;
        if (body.NodeType == ExpressionType.Convert)
        {
            body = ((UnaryExpression)body).Operand;
        }

        if (body is MethodCallExpression call && call.Method.DeclaringType == typeof(SQLiteColumn))
        {
            return (string)ExpressionHelpers.GetConstantValue(call.Arguments[1])!;
        }

        if (body is MemberExpression member
            && mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name) is { } mapped)
        {
            return mapped.Name;
        }

        throw new ArgumentException(
            "The Set target must be a property on the entity or SQLiteColumn.Of<TValue>(row, \"Name\").", nameof(column));
    }

    /// <summary>
    /// Builds a one-row sub-select that evaluates a set of operands once and exposes them as named
    /// columns, so a body that needs an operand in several places does not repeat the operand SQL.
    /// </summary>
    public static SQLiteExpression EvaluateOnce(SQLiteCounters counters, Type type, SQLiteExpression[] operands, Func<SQLiteExpression[], SQLiteExpression> buildBody)
    {
        string[] names = new string[operands.Length];
        SQLiteExpression[] aliases = new SQLiteExpression[operands.Length];
        for (int i = 0; i < operands.Length; i++)
        {
            names[i] = "v" + counters.NextIdentifier();
            aliases[i] = SQLiteExpression.Leaf(operands[i].Type, counters.NextIdentifier(), names[i]);
        }

        SQLiteExpression body = buildBody(aliases);

        SQLiteExpression[] children = new SQLiteExpression[operands.Length + 1];
        children[0] = body;
        for (int i = 0; i < operands.Length; i++)
        {
            children[i + 1] = operands[i];
        }

        string[] parts = new string[operands.Length + 2];
        parts[0] = "(SELECT ";
        parts[1] = " FROM (SELECT ";
        for (int i = 0; i < operands.Length; i++)
        {
            parts[i + 2] = " AS " + names[i] + (i == operands.Length - 1 ? "))" : ", ");
        }

        SQLiteExpression[] paramOrder = new SQLiteExpression[operands.Length + 1];
        for (int i = 0; i < operands.Length; i++)
        {
            paramOrder[i] = operands[i];
        }
        paramOrder[operands.Length] = body;

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(paramOrder);
        return SQLiteExpression.Multi(type, counters.NextIdentifier(), parts, children, parameters);
    }

    /// <summary>
    /// Translates a parameterless lambda body into a SQL fragment with all parameter values inlined
    /// as literals. Used to convert default-value expressions for DDL clauses where SQLite does not
    /// accept placeholders (CREATE TABLE column DEFAULT, ALTER TABLE ADD COLUMN DEFAULT).
    /// </summary>
    public static string Translate(SQLiteDatabase database, LambdaExpression lambda, string parameterName)
    {
        Expression body = lambda.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : lambda.Body;

        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        Expression result = visitor.Visit(body);
        if (result is not SQLiteExpression sqlExpr)
        {
            throw new ArgumentException($"Default expression '{lambda}' could not be translated to SQL.", parameterName);
        }

        return SqlLiteralHelper.InlineParameters(sqlExpr.ToString(), sqlExpr.Parameters ?? [], database.Options);
    }

    /// <summary>
    /// Builds the column definition SQL for a <see cref="TableColumn" /> as it appears inside
    /// <c>CREATE TABLE</c> or <c>ALTER TABLE ADD COLUMN</c>.
    /// </summary>
    public static string GetCreateColumnSql(TableColumn column, bool emitInlinePrimaryKey = true, string? defaultOverride = null, bool emitForeignKey = true)
    {
        string columnType = column.ColumnType.ToString().ToUpperInvariant();
        bool inlinePk = emitInlinePrimaryKey && column.IsPrimaryKey;
        bool rowidAlias = inlinePk && column.ColumnType == SQLiteColumnType.Integer;
        string nullability;
        if (!inlinePk)
        {
            nullability = column.IsNullable ? "NULL" : "NOT NULL";
        }
        else
        {
            nullability = rowidAlias ? string.Empty : "NOT NULL";
        }
        string primaryKey = inlinePk ? "PRIMARY KEY" : string.Empty;
        string autoIncrement = inlinePk && column.IsAutoIncrement ? "AUTOINCREMENT" : string.Empty;

        StringBuilder sb = new();
        sb.Append(IdentifierGuard.Quote(column.Name));
        sb.Append(' ');
        sb.Append(columnType);
        if (nullability.Length > 0)
        {
            sb.Append(' ');
            sb.Append(nullability);
        }
        if (primaryKey.Length > 0)
        {
            sb.Append(' ');
            sb.Append(primaryKey);
        }
        if (autoIncrement.Length > 0)
        {
            sb.Append(' ');
            sb.Append(autoIncrement);
        }
        if (column.ForeignKey != null && emitForeignKey)
        {
            sb.Append(' ');
            ForeignKeySql.WriteSql(column.ForeignKey, sb, inline: true);
        }

        string? defaultSql = defaultOverride ?? column.DefaultSql;
        if (defaultSql != null)
        {
            sb.Append(" DEFAULT ");
            sb.Append(defaultSql);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders an index column's direction as the SQL clause to append after the column name.
    /// </summary>
    public static string Clause(SQLiteIndexDirection direction)
    {
        return direction switch
        {
            SQLiteIndexDirection.Inherit => string.Empty,
            SQLiteIndexDirection.Ascending => " ASC",
            SQLiteIndexDirection.Descending => " DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }

    /// <summary>
    /// Renders a column's collation as the SQL clause to append after the column name.
    /// </summary>
    public static string Clause(SQLiteCollation collation)
    {
        return collation switch
        {
            SQLiteCollation.Inherit => string.Empty,
            SQLiteCollation.Binary => " COLLATE BINARY",
            SQLiteCollation.NoCase => " COLLATE NOCASE",
            SQLiteCollation.Rtrim => " COLLATE RTRIM",
            _ => throw new ArgumentOutOfRangeException(nameof(collation), collation, null),
        };
    }

    /// <summary>
    /// Renders a SQLite version number (the format returned by
    /// <c>sqlite3_libversion_number()</c>, e.g. <c>3032000</c>) as a human-readable
    /// <c>major.minor.patch</c> string.
    /// </summary>
    public static string Format(int versionNumber)
    {
        int major = versionNumber / 1_000_000;
        int minor = versionNumber / 1_000 % 1_000;
        int patch = versionNumber % 1_000;
        return $"{major}.{minor}.{patch}";
    }
}
