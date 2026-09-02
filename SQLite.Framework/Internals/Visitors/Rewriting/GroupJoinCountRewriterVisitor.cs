namespace SQLite.Framework.Internals.Visitors.Rewriting;

internal sealed class GroupJoinCountRewriterVisitor : ExpressionVisitor
{
    private readonly SQLiteCounters counters;
    private readonly ParameterExpression group;
    private readonly SQLiteExpression innerSource;
    private readonly SQLiteExpression onClause;

    public GroupJoinCountRewriterVisitor(ParameterExpression group, SQLiteExpression innerSource, SQLiteExpression onClause, SQLiteCounters counters)
    {
        this.group = group;
        this.innerSource = innerSource;
        this.onClause = onClause;
        this.counters = counters;
    }

    public bool Found { get; private set; }

    public bool UnsupportedUsage { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count == 1
            && node.Arguments[0] == group
            && node.Method.DeclaringType == typeof(Enumerable)
            && node.Method.Name is nameof(Enumerable.Count) or nameof(Enumerable.LongCount))
        {
            Found = true;
            return SQLiteExpression.Multi(
                node.Type,
                counters.NextIdentifier(),
                ["(SELECT COUNT(*) FROM ", " WHERE ", ")"],
                [innerSource, onClause],
                ParameterHelpers.CombineParameters(innerSource, onClause));
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == group)
        {
            UnsupportedUsage = true;
        }

        return node;
    }
}
