namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private MethodCallExpression VisitTake(MethodCallExpression node)
    {
        ThrowIfReverse(node.Method.Name);

        int n = Math.Max(0, (int)ExpressionHelpers.GetConstantValue(node.Arguments[1])!);
        Take = Take.HasValue ? Math.Min(Take.Value, n) : n;
        return node;
    }

    private MethodCallExpression VisitSkip(MethodCallExpression node)
    {
        ThrowIfReverse(node.Method.Name);

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
            ? " ASC"
            : " DESC";

        if (node.Arguments.Count == 3)
        {
            SQLiteNullsOrder nulls = (SQLiteNullsOrder)ExpressionHelpers.GetConstantValue(node.Arguments[2])!;
            if (nulls != SQLiteNullsOrder.Default)
            {
#if SQLITE_FRAMEWORK_VERSION_AWARE
                database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "NULLS FIRST/LAST in ORDER BY");
#endif
                order += nulls == SQLiteNullsOrder.First ? " NULLS FIRST" : " NULLS LAST";
            }
        }

        OrderBys.Add(SQLiteExpression.Wrap(node.Arguments[1].Type, visitor.Counters.NextIdentifier(), "", sqlExpression, order, sqlExpression.Parameters));
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

    private MethodCallExpression VisitElementAt(MethodCallExpression node, bool throwOnEmpty)
    {
        ThrowIfReverse(node.Method.Name);

        object indexValue = ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        int n = indexValue switch
        {
            int i => i,
            Index idx when !idx.IsFromEnd => idx.Value,
            _ => throw new NotSupportedException(
                $"{node.Method.Name} with an Index from the end is not supported. " +
                "Use OrderByDescending instead so you can index from the start.")
        };

        Skip = (Skip ?? 0) + n;
        Take = Take.HasValue ? Math.Min(Math.Max(0, Take.Value - n), 1) : 1;
        if (throwOnEmpty)
        {
            ThrowOnEmpty = true;
        }

        return node;
    }

    private void ThrowIfReverse(string methodName)
    {
        if (Reverse)
        {
            throw new NotSupportedException(
                $"{methodName} after Reverse is not supported because Reverse is applied in memory after the SQL query runs, " +
                "which would pick the wrong rows. Use OrderByDescending instead, or call ToList()/ToArray() first.");
        }
    }
}
