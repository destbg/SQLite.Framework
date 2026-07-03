namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it reads a TimeSpan column of a table row.
/// Used under Text TimeSpan storage to tell a fully computable TimeSpan argument, such as
/// TimeSpan.FromHours over a number column, apart from one that reads a stored text value,
/// which cannot take part in tick arithmetic.
/// </summary>
internal sealed class TimeSpanColumnFinderVisitor : ExpressionVisitor
{
    private readonly SQLVisitor owner;

    public TimeSpanColumnFinderVisitor(SQLVisitor owner)
    {
        this.owner = owner;
    }

    public bool Found { get; private set; }

    protected override Expression VisitMember(MemberExpression node)
    {
        if ((Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(TimeSpan)
            && !ExpressionHelpers.IsConstant(node)
            && owner.TryResolveColumnLeaf(node) != null)
        {
            Found = true;
            return node;
        }

        return base.VisitMember(node);
    }
}
