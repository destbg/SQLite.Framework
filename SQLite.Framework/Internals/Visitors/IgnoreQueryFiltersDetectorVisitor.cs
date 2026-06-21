namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a LINQ expression tree to detect whether it contains any
/// <see cref="QueryableExtensions.IgnoreQueryFilters{T}" /> call. When one is present, the registered
/// query filters are dropped for the whole query, including joined tables and subqueries, no matter
/// where in the chain the call appears.
/// </summary>
internal sealed class IgnoreQueryFiltersDetectorVisitor : ExpressionVisitor
{
    public bool Found { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (QueryFilterInjector.IsIgnoreQueryFiltersCall(node))
        {
            Found = true;
            return node;
        }

        return base.VisitMethodCall(node);
    }
}
