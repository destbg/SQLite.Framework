namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Walks a query expression before translation and reserves the user-supplied parameter names of
/// every <c>FromSql</c> fragment, so the translator's generated names never collide with them.
/// Without this, a fragment that translates after the outer clauses, such as a set operation
/// operand or a join side, would reserve its names too late and the last bound value would win.
/// </summary>
internal sealed class FromSqlParameterReserver : ExpressionVisitor
{
    private readonly SQLiteCounters counters;
    private readonly HashSet<object> visited = [];

    private FromSqlParameterReserver(SQLiteCounters counters)
    {
        this.counters = counters;
    }

    public static void Reserve(Expression node, SQLiteCounters counters)
    {
        new FromSqlParameterReserver(counters).Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is SQLiteCte cte && visited.Add(cte))
        {
            Visit(cte.Query.Body);
        }
        else if (node.Value is IQueryable queryable && visited.Add(queryable))
        {
            Visit(queryable.Expression);
        }

        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(SQLiteDatabase.FromSql)
            && node.Arguments.Count == 2
            && ExpressionHelpers.IsConstant(node.Arguments[1])
            && ExpressionHelpers.GetConstantValue(node.Arguments[1]) is IEnumerable<object> parameters)
        {
            counters.ReserveParamNames(parameters.OfType<SQLiteParameter>().Select(p => p.Name));
        }

        return base.VisitMethodCall(node);
    }
}
