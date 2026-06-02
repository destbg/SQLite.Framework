namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Translates a parameterless lambda body into a SQL fragment with all parameter values inlined
/// as literals. Used to convert default-value expressions for DDL clauses where SQLite does not
/// accept placeholders (CREATE TABLE column DEFAULT, ALTER TABLE ADD COLUMN DEFAULT).
/// </summary>
internal static class DefaultExpressionTranslator
{
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
}
