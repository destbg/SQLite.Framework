namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a LINQ expression tree to detect whether it contains any
/// <see cref="QueryableExtensions.IgnoreQueryFilters{T}" /> call. When one is present, the registered
/// query filters are dropped for the whole query, including joined tables and subqueries, no matter
/// where in the chain the call appears.
/// </summary>
internal sealed class IgnoreQueryFiltersDetectorVisitor : ExpressionVisitor
{
    private readonly HashSet<object> visited = [];

    public bool Found { get; private set; }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        DescendIntoValue(node.Value);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!Found
            && (node.Type.IsAssignableTo(typeof(SQLiteCte)) || node.Type.IsAssignableTo(typeof(IQueryable)))
            && ExpressionHelpers.IsConstant(node))
        {
            DescendIntoValue(ExpressionHelpers.GetConstantValue(node));
            return node;
        }

        return base.VisitMember(node);
    }

    private void DescendIntoValue(object? value)
    {
        if (Found)
        {
            return;
        }

        if (value is SQLiteCte cte && visited.Add(cte))
        {
            Visit(cte.Query.Body);
        }
        else if (value is IQueryable queryable && visited.Add(queryable))
        {
            Visit(queryable.Expression);
        }
    }

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
