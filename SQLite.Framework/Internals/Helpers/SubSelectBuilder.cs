namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds a one-row sub-select that evaluates a set of operands once and exposes them as named
/// columns, so a body that needs an operand in several places does not repeat the operand SQL.
/// </summary>
internal static class SubSelectBuilder
{
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
}
