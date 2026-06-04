namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites an operand of a client-evaluated projection so that only leaf table columns become
/// SQL (read back as columns), while every operation is kept as a C# expression and computed at
/// materialization time. This keeps the SELECT list to raw leaf columns, which is the same shape
/// the source generator reads, so reflection and generated materializers agree.
/// </summary>
internal sealed class ClientLeafRewriter : ExpressionVisitor
{
    private readonly SQLVisitor owner;

    public ClientLeafRewriter(SQLVisitor owner)
    {
        this.owner = owner;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is null || ExpressionHelpers.IsConstant(node))
        {
            return node;
        }

        if (node is MemberExpression or ParameterExpression && owner.TryResolveColumnLeaf(node) is { } leaf)
        {
            return leaf;
        }

        return base.Visit(node);
    }
}
