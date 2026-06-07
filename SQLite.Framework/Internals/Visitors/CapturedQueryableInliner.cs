namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a LINQ expression tree before translation and replaces every reference to a
/// <see cref="Queryable{T}" /> wrapper with the query expression the wrapper holds.
/// </summary>
internal sealed class CapturedQueryableInliner : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        if (ExpressionHelpers.IsConstant(node)
            && typeof(IQueryable).IsAssignableFrom(node.Type)
            && ExpressionHelpers.GetConstantValue(node) is IChainQueryable chain)
        {
            return Visit(((BaseSQLiteQueryable)chain).Expression);
        }

        return base.VisitMember(node);
    }

    public static Expression Inline(Expression node)
    {
        return new CapturedQueryableInliner().Visit(node);
    }
}
