namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private MethodCallExpression VisitTake(MethodCallExpression node)
    {
        ThrowIfReverse(node.Method.Name);

        bool clientSide = IsDistinct && LastSelectIsClient && !IsInnerQuery;
        long? take = clientSide ? ClientTake : Take;
        long? skip = clientSide ? ClientSkip : Skip;

        object value = ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        if (value is Range range)
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd)
            {
                throw new NotSupportedException(
                    $"{node.Method.Name} with a Range that indexes from the end is not supported because the row count is not known before the query runs.");
            }

            int start = range.Start.Value;
            int length = Math.Max(0, range.End.Value - start);
            skip = (skip ?? 0) + start;
            if (take.HasValue)
            {
                take = Math.Max(0, take.Value - start);
            }

            take = take.HasValue ? Math.Min(take.Value, length) : length;
        }
        else
        {
            int n = Math.Max(0, (int)value);
            take = take.HasValue ? Math.Min(take.Value, n) : n;
        }

        if (clientSide)
        {
            ClientTake = take;
            ClientSkip = skip;
        }
        else
        {
            Take = take;
            Skip = skip;
        }

        return node;
    }

    private MethodCallExpression VisitSkip(MethodCallExpression node)
    {
        ThrowIfReverse(node.Method.Name);

        int n = Math.Max(0, (int)ExpressionHelpers.GetConstantValue(node.Arguments[1])!);
        if (IsDistinct && LastSelectIsClient && !IsInnerQuery)
        {
            ClientSkip = (ClientSkip ?? 0) + n;
            if (ClientTake.HasValue)
            {
                ClientTake = Math.Max(0, ClientTake.Value - n);
            }

            return node;
        }

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
        ThrowIfGroupJoinGroupPredicate(lambda.Body);
        bool previousFtsMatchAsSubquery = visitor.FtsMatchAsSubquery;
        visitor.FtsMatchAsSubquery = true;
        Expression orderBy = visitor.Visit(lambda.Body);
        visitor.FtsMatchAsSubquery = previousFtsMatchAsSubquery;

        if (orderBy is not SQLiteExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported ORDER BY expression {lambda.Body}");
        }

        sqlExpression = visitor.CoalesceLiftedOrderComparison(lambda.Body, sqlExpression);
        sqlExpression = visitor.CastTextDecimalForOrdering(sqlExpression);

        if (node.Method.Name is nameof(System.Linq.Queryable.OrderBy) or nameof(System.Linq.Queryable.OrderByDescending))
        {
            OrderBys.Clear();
            Reverse = false;
            ReverseBeforeDistinct = false;
        }

        string baseDirection = node.Method.Name is nameof(System.Linq.Queryable.OrderBy) or nameof(System.Linq.Queryable.ThenBy)
            ? " ASC"
            : " DESC";
        string nullsClause = string.Empty;

        if (node.Arguments.Count == 3)
        {
            if (node.Arguments[2].Type != typeof(SQLiteNullsOrder))
            {
                throw new NotSupportedException(
                    $"{node.Method.Name} with a custom IComparer is not supported. " +
                    "Ordering runs inside SQLite, which cannot call a .NET comparer. " +
                    "Remove the comparer to use the default SQL ordering or call ToList()/ToArray() first to order in memory.");
            }

            SQLiteNullsOrder nulls = (SQLiteNullsOrder)ExpressionHelpers.GetConstantValue(node.Arguments[2])!;
            if (nulls != SQLiteNullsOrder.Default)
            {
#if SQLITE_FRAMEWORK_VERSION_AWARE
                database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "NULLS FIRST/LAST in ORDER BY");
#endif
                nullsClause = nulls == SQLiteNullsOrder.First ? " NULLS FIRST" : " NULLS LAST";
            }
        }

        Type orderKeyType = Nullable.GetUnderlyingType(sqlExpression.Type) ?? sqlExpression.Type;
        if (orderKeyType.IsEnum)
        {
            orderKeyType = Enum.GetUnderlyingType(orderKeyType);
        }

        if (orderKeyType == typeof(ulong))
        {
            OrderBys.Add(SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "(", sqlExpression, ") < 0" + baseDirection + nullsClause, sqlExpression.Parameters));
            OrderBys.Add(SQLiteExpression.Wrap(node.Arguments[1].Type, visitor.Counters.NextIdentifier(), "", sqlExpression, baseDirection, sqlExpression.Parameters));
            return orderBy;
        }

        OrderBys.Add(SQLiteExpression.Wrap(node.Arguments[1].Type, visitor.Counters.NextIdentifier(), "", sqlExpression, baseDirection + nullsClause, sqlExpression.Parameters));
        return orderBy;
    }

    private MethodCallExpression VisitDistinct(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);
        ComparerArgumentGuard.ThrowIfComparer(node);

        bool alreadyDistinct = IsDistinct;
        IsDistinct = true;
        if (Reverse && !ReverseBeforeDistinct && !alreadyDistinct)
        {
            ReverseBeforeDistinct = true;
            Reverse = false;
        }

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
        IsRowSelector = true;

        object indexValue = ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        int n = indexValue switch
        {
            int i => i,
            Index idx when !idx.IsFromEnd => idx.Value,
            _ => throw new NotSupportedException(
                $"{node.Method.Name} with an Index from the end is not supported. " +
                "Use OrderByDescending instead so you can index from the start.")
        };

        if (IsDistinct && LastSelectIsClient)
        {
            if (n < 0)
            {
                ClientTake = 0;
            }
            else
            {
                ClientSkip = (ClientSkip ?? 0) + n;
                ClientTake = ClientTake.HasValue ? Math.Min(Math.Max(0, ClientTake.Value - n), 1) : 1;
            }
        }
        else if (n < 0)
        {
            Take = 0;
        }
        else
        {
            Skip = (Skip ?? 0) + n;
            Take = Take.HasValue ? Math.Min(Math.Max(0, Take.Value - n), 1) : 1;
        }

        if (throwOnEmpty)
        {
            ThrowOnEmpty = true;
            ElementAtSemantic = true;
        }

        return node;
    }

    private void ThrowIfReverse(string methodName)
    {
        if (Reverse || (ReverseBeforeDistinct && !LastSelectIsClient))
        {
            throw new NotSupportedException(
                $"{methodName} after Reverse is not supported because Reverse is applied in memory after the SQL query runs, " +
                "which would pick the wrong rows. Use OrderByDescending instead or call ToList()/ToArray() first.");
        }
    }
}
