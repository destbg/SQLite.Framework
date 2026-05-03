namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private MethodCallExpression VisitTake(MethodCallExpression node)
    {
        int n = (int)ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        Take = Take.HasValue ? Math.Min(Take.Value, n) : n;
        return node;
    }

    private MethodCallExpression VisitSkip(MethodCallExpression node)
    {
        int n = (int)ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        Skip = (Skip ?? 0) + n;
        if (Take.HasValue)
        {
            Take = Math.Max(0, Take.Value - n);
        }
        return node;
    }

    private Expression VisitOrder(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        Expression orderBy = visitor.Visit(lambda.Body);

        if (orderBy is not SQLiteExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported ORDER BY expression {lambda.Body}");
        }

        if (node.Method.Name is nameof(System.Linq.Queryable.OrderBy) or nameof(System.Linq.Queryable.OrderByDescending))
        {
            OrderBys.Clear();
        }

        string order = node.Method.Name is nameof(System.Linq.Queryable.OrderBy) or nameof(System.Linq.Queryable.ThenBy)
            ? "ASC"
            : "DESC";

        OrderBys.Add(new SQLiteExpression(node.Arguments[1].Type, visitor.Counters.IdentifierIndex++, $"{sqlExpression.Sql} {order}", sqlExpression.Parameters));
        return orderBy;
    }

    private MethodCallExpression VisitDistinct(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        IsDistinct = true;
        return node;
    }

    private MethodCallExpression VisitReverse(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        Reverse = !Reverse;
        return node;
    }
}
