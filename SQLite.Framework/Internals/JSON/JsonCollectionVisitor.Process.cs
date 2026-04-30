namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private static readonly Dictionary<string, Action<JsonCollectionVisitor, MethodCallExpression, Type>> Handlers = new()
    {
        [nameof(Enumerable.Where)] = static (v, c, t) => v.HandleWhere(c, t),
        [nameof(Enumerable.OrderBy)] = static (v, c, t) => v.HandleOrderBy(c, t, "ASC", clear: true),
        [nameof(Enumerable.OrderByDescending)] = static (v, c, t) => v.HandleOrderBy(c, t, "DESC", clear: true),
        [nameof(Enumerable.ThenBy)] = static (v, c, t) => v.HandleOrderBy(c, t, "ASC", clear: false),
        [nameof(Enumerable.ThenByDescending)] = static (v, c, t) => v.HandleOrderBy(c, t, "DESC", clear: false),
        [nameof(Enumerable.GroupBy)] = static (v, c, t) => v.HandleGroupBy(c, t),
        [nameof(Enumerable.Select)] = static (v, c, t) => v.HandleSelect(c, t),
        [nameof(Enumerable.SelectMany)] = static (v, c, t) => v.HandleSelectMany(c, t),
        [nameof(Enumerable.Skip)] = static (v, c, _) => v.HandleSkip(c),
        [nameof(Enumerable.Take)] = static (v, c, _) => v.HandleTake(c),
        [nameof(Enumerable.First)] = static (v, c, t) => v.HandleFirst(c, t),
        [nameof(Enumerable.FirstOrDefault)] = static (v, c, t) => v.HandleFirst(c, t),
        [nameof(Enumerable.Last)] = static (v, c, t) => v.HandleLast(c, t),
        [nameof(Enumerable.LastOrDefault)] = static (v, c, t) => v.HandleLast(c, t),
        [nameof(Enumerable.Single)] = static (v, c, t) => v.HandleSingle(c, t),
        [nameof(Enumerable.SingleOrDefault)] = static (v, c, t) => v.HandleSingle(c, t),
        [nameof(Enumerable.Count)] = static (v, c, t) => v.HandleCount(c, t),
        [nameof(Enumerable.Any)] = static (v, c, t) => v.HandleAny(c, t),
        [nameof(Enumerable.All)] = static (v, c, t) => v.HandleAll(c, t),
        [nameof(Enumerable.Min)] = static (v, c, t) => v.HandleAggregate(c, t, "MIN"),
        [nameof(Enumerable.Max)] = static (v, c, t) => v.HandleAggregate(c, t, "MAX"),
        [nameof(Enumerable.Sum)] = static (v, c, t) => v.HandleAggregate(c, t, "SUM"),
        [nameof(Enumerable.Average)] = static (v, c, t) => v.HandleAggregate(c, t, "AVG"),
        [nameof(Enumerable.Distinct)] = static (v, _, _) => v.HandleDistinct(),
        [nameof(Enumerable.Reverse)] = static (v, _, _) => v.HandleReverse(),
        [nameof(Enumerable.ElementAt)] = static (v, c, _) => v.HandleElementAt(c),
        [nameof(Enumerable.Contains)] = static (v, c, _) => v.HandleContains(c),
    };

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void ProcessMethod(MethodCallExpression call, Type sourceType)
    {
        Type elementType = TypeHelpers.GetEnumerableElementType(sourceType)!;
        Handlers[call.Method.Name](this, call, elementType);
    }

    private void HandleWhere(MethodCallExpression call, Type elementType)
    {
        wheres.Add(VisitLambda(call.Arguments[1], elementType));
    }

    private void HandleOrderBy(MethodCallExpression call, Type elementType, string direction, bool clear)
    {
        if (clear)
        {
            orderBys.Clear();
        }

        orderBys.Add($"{VisitLambda(call.Arguments[1], elementType)} {direction}");
    }

    private void HandleGroupBy(MethodCallExpression call, Type elementType)
    {
        groupBys.Add(VisitLambda(call.Arguments[1], elementType));
    }

    private void HandleSelect(MethodCallExpression call, Type elementType)
    {
        selectExpr = VisitLambda(call.Arguments[1], elementType);
    }

    private void HandleSelectMany(MethodCallExpression call, Type elementType)
    {
        string selSql = VisitLambdaAliased(call.Arguments[1], elementType, "e");
        crossJoin = $", json_each({selSql}) n";
        selectExpr = "n.value";
    }

    private void HandleSkip(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        offset = arg.Sql!;
        AddParameters(arg);
    }

    private void HandleTake(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        limit = arg.Sql!;
        AddParameters(arg);
    }

    private void AddOptionalPredicate(MethodCallExpression call, Type elementType)
    {
        if (call.Arguments.Count > 1)
        {
            wheres.Add(VisitLambda(call.Arguments[1], elementType));
        }
    }

    private void HandleFirst(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        limit = "1";
        wrapInArray = false;
    }

    private void HandleLast(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        ReverseOrderBys();
        limit = "1";
        wrapInArray = false;
    }

    private void HandleSingle(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        singleSemantic = true;
        wrapInArray = false;
    }

    private void HandleCount(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        selectExpr = "COUNT(*)";
        wrapInArray = false;
    }

    private void HandleAny(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        existsWrapper = "EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }

    private void HandleAll(MethodCallExpression call, Type elementType)
    {
        wheres.Add($"NOT ({VisitLambda(call.Arguments[1], elementType)})");
        existsWrapper = "NOT EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }

    private void HandleAggregate(MethodCallExpression call, Type elementType, string sqlFunc)
    {
        string inner = call.Arguments.Count > 1
            ? VisitLambda(call.Arguments[1], elementType)
            : "value";

        selectExpr = $"{sqlFunc}({inner})";
        wrapInArray = false;
    }

    private void HandleDistinct()
    {
        distinct = true;
    }

    private void HandleReverse()
    {
        ReverseOrderBys();
    }

    private void ReverseOrderBys()
    {
        if (orderBys.Count == 0)
        {
            orderBys.Add("key DESC");
            return;
        }

        for (int i = 0; i < orderBys.Count; i++)
        {
            orderBys[i] = orderBys[i].EndsWith(" ASC")
                ? orderBys[i][..^4] + " DESC"
                : orderBys[i][..^5] + " ASC";
        }
    }

    private void HandleElementAt(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        offset = arg.Sql!;
        AddParameters(arg);
        limit = "1";
        wrapInArray = false;
    }

    private void HandleContains(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        AddParameters(arg);
        wheres.Add($"value = {arg.Sql}");
        existsWrapper = "EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }
}
