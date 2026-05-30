namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Translates a lambda over an entity into a bare SQL fragment that uses unqualified column names
/// and inlined literals instead of bound parameters. Used by the DDL-style spots that cannot bind
/// parameters and must match each other byte for byte: CHECK constraints, computed columns,
/// partial index filters, and the WHERE on an UPSERT conflict target.
/// </summary>
internal static class BareSqlTranslator
{
    public static string Translate(SQLiteDatabase database, TableMapping mapping, LambdaExpression lambda)
    {
        return Translate(database, mapping, lambda.Parameters[0], lambda.Body);
    }

    public static string Translate(SQLiteDatabase database, TableMapping mapping, ParameterExpression rowParameter, Expression body)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        visitor.MethodArguments[rowParameter] = RowColumns(visitor, mapping, null);
        return Finish(visitor, body);
    }

    public static string TranslateUpdateWhere(SQLiteDatabase database, TableMapping mapping, LambdaExpression lambda)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);
        visitor.MethodArguments[lambda.Parameters[0]] = RowColumns(visitor, mapping, null);

        if (lambda.Parameters.Count > 1)
        {
            visitor.MethodArguments[lambda.Parameters[1]] = RowColumns(visitor, mapping, "excluded.");
        }

        return Finish(visitor, lambda.Body);
    }

    private static Dictionary<string, Expression> RowColumns(SQLVisitor visitor, TableMapping mapping, string? prefix)
    {
        return mapping.Columns.ToDictionary(
            c => c.PropertyInfo.Name,
            Expression (c) => SQLiteExpression.Leaf(c.PropertyType, visitor.Counters.NextIdentifier(), prefix + IdentifierGuard.Quote(c.Name)));
    }

    private static string Finish(SQLVisitor visitor, Expression body)
    {
        Expression result = visitor.Visit(body);
        if (result is not SQLiteExpression sqlExpr)
        {
            throw new ArgumentException($"Expression '{body}' could not be translated to SQL.", nameof(body));
        }

        return SqlLiteralHelper.InlineParameters(sqlExpr.ToString(), sqlExpr.Parameters ?? []);
    }
}
