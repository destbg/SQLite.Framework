namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it reads a group join group that has not been
/// flattened into a join. Such a group only exists in memory, so a predicate or ordering that
/// uses it cannot run inside SQLite.
/// </summary>
internal sealed class GroupJoinGroupUsageVisitor : ExpressionVisitor
{
    private readonly IReadOnlyCollection<Type> groupElementTypes;

    public GroupJoinGroupUsageVisitor(IReadOnlyCollection<Type> groupElementTypes)
    {
        this.groupElementTypes = groupElementTypes;
    }

    public bool Found { get; private set; }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (IsGroupReference(node))
        {
            Found = true;
            return node;
        }

        return base.VisitMember(node);
    }

    private bool IsGroupReference(MemberExpression node)
    {
        if (!node.Type.IsGenericType || node.Type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            return false;
        }

        if (!groupElementTypes.Contains(node.Type.GetGenericArguments()[0]))
        {
            return false;
        }

        (string _, ParameterExpression? parameter) = ExpressionHelpers.ResolveNullableParameterPath(node);
        return parameter != null;
    }
}
