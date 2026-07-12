namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Translates a lambda over an entity into a bare SQL fragment that uses unqualified column names
/// and inlined literals instead of bound parameters. Used by the DDL-style spots that cannot bind
/// parameters and must match each other byte for byte: CHECK constraints, computed columns,
/// partial index filters and the WHERE and SET clauses on an UPSERT DO UPDATE.
/// </summary>
internal static class BareSqlTranslator
{
    public static string Translate(SQLiteDatabase database, TableMapping mapping, LambdaExpression lambda, bool wrapConverterReads)
    {
        return Translate(database, mapping, lambda.Parameters[0], lambda.Body, wrapConverterReads);
    }

    public static string Translate(SQLiteDatabase database, TableMapping mapping, ParameterExpression rowParameter, Expression body, bool wrapConverterReads)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        visitor.MethodArguments[rowParameter] = RowColumns(visitor, rowParameter, mapping, null, wrapConverterReads);
        return Finish(visitor, body);
    }

    public static string TranslateUpdateRowExpression(SQLiteDatabase database, TableMapping mapping, LambdaExpression lambda, bool wrapConverterReads)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        visitor.MethodArguments[lambda.Parameters[0]] = RowColumns(visitor, lambda.Parameters[0], mapping, null, wrapConverterReads);

        if (lambda.Parameters.Count > 1)
        {
            visitor.MethodArguments[lambda.Parameters[1]] = RowColumns(visitor, lambda.Parameters[1], mapping, "excluded.", wrapConverterReads);
        }

        return Finish(visitor, lambda.Body);
    }

    /// <summary>
    /// Translates one expression from a LINQ-typed trigger body. Each entry in
    /// <paramref name="rows" /> binds a parameter to a table's columns with an optional prefix:
    /// no prefix for the statement's target row (bare column names), <c>OLD.</c> and <c>NEW.</c> for
    /// the trigger's old and new rows.
    /// </summary>
    public static string TranslateTrigger(SQLiteDatabase database, Expression body, (ParameterExpression Parameter, TableMapping Mapping, string? Prefix)[] rows, bool wrapConverterReads)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        foreach ((ParameterExpression parameter, TableMapping mapping, string? prefix) in rows)
        {
            visitor.MethodArguments[parameter] = RowColumns(visitor, parameter, mapping, prefix, wrapConverterReads);
        }

        return Finish(visitor, body);
    }

    private static Dictionary<string, Expression> RowColumns(SQLVisitor visitor, ParameterExpression parameter, TableMapping mapping, string? prefix, bool wrapConverterReads)
    {
        visitor.RowColumnPrefixes[parameter] = prefix;
        return mapping.Columns.ToDictionary(
            c => c.PropertyInfo.Name,
            Expression (c) => SQLiteExpression.Leaf(c.PropertyType, visitor.Counters.NextIdentifier(), ColumnSql(visitor, c, prefix, wrapConverterReads)));
    }

    private static string ColumnSql(SQLVisitor visitor, TableColumn column, string? prefix, bool wrapConverterReads)
    {
        string sql = prefix + IdentifierGuard.Quote(column.Name);
        if (wrapConverterReads
            && visitor.Database.Options.TypeConverters.TryGetValue(column.PropertyType, out ISQLiteTypeConverter? converter)
            && converter.ColumnSqlExpression is { } columnExpr)
        {
            return string.Format(columnExpr, sql);
        }

        return sql;
    }

    private static string Finish(SQLVisitor visitor, Expression body)
    {
        Expression result = visitor.Visit(body);
        if (result is not SQLiteExpression sqlExpr)
        {
            throw new ArgumentException($"Expression '{body}' could not be translated to SQL.", nameof(body));
        }

        return SqlLiteralHelper.InlineParameters(sqlExpr.ToString(), sqlExpr.Parameters ?? [], visitor.Database.Options);
    }
}
