namespace SQLite.Framework.Internals.Visitors;

internal sealed class GroupSequenceUsageWalker : ExpressionVisitor
{
    private readonly ParameterExpression group;

    public GroupSequenceUsageWalker(ParameterExpression group)
    {
        this.group = group;
    }

    public bool UsesGroupAsSequence { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count > 0 && node.Arguments[0] == group)
        {
            UsesGroupAsSequence = true;
            return node;
        }

        return base.VisitMethodCall(node);
    }
}
