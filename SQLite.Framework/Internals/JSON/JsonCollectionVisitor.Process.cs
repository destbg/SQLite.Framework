namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private void ProcessMethod(MethodCallExpression call)
    {
        if (RequiresWindowMaterialization(call.Method.Name))
        {
            MaterializeWindow();
        }

        switch (call.Method.Name)
        {
            case nameof(Enumerable.Where):
                HandleWhere(call, currentElementType);
                break;
            case nameof(Enumerable.OrderBy):
                HandleOrderBy(call, currentElementType, "ASC", clear: true);
                break;
            case nameof(Enumerable.OrderByDescending):
                HandleOrderBy(call, currentElementType, "DESC", clear: true);
                break;
            case nameof(Enumerable.ThenBy):
                HandleOrderBy(call, currentElementType, "ASC", clear: false);
                break;
            case nameof(Enumerable.ThenByDescending):
                HandleOrderBy(call, currentElementType, "DESC", clear: false);
                break;
            case nameof(Enumerable.GroupBy):
                HandleGroupBy(call, currentElementType);
                break;
            case nameof(Enumerable.Select):
                HandleSelect(call, currentElementType);
                break;
            case nameof(Enumerable.SelectMany):
                HandleSelectMany(call, currentElementType);
                break;
            case nameof(Enumerable.Skip):
                HandleSkip(call);
                break;
            case nameof(Enumerable.Take):
                HandleTake(call);
                break;
            case nameof(Enumerable.First):
            case nameof(Enumerable.FirstOrDefault):
                HandleFirst(call, currentElementType);
                break;
            case nameof(Enumerable.Last):
            case nameof(Enumerable.LastOrDefault):
                HandleLast(call, currentElementType);
                break;
            case nameof(Enumerable.Single):
            case nameof(Enumerable.SingleOrDefault):
                HandleSingle(call, currentElementType);
                break;
            case nameof(Enumerable.Count):
                HandleCount(call, currentElementType);
                break;
            case nameof(Enumerable.Any):
                HandleAny(call, currentElementType);
                break;
            case nameof(Enumerable.All):
                HandleAll(call, currentElementType);
                break;
            case nameof(Enumerable.Min):
                HandleAggregate(call, currentElementType, "MIN");
                break;
            case nameof(Enumerable.Max):
                HandleAggregate(call, currentElementType, "MAX");
                break;
            case nameof(Enumerable.Sum):
                HandleAggregate(call, currentElementType, "SUM");
                break;
            case nameof(Enumerable.Average):
                HandleAggregate(call, currentElementType, "AVG");
                break;
            case nameof(Enumerable.Distinct):
                HandleDistinct();
                break;
            case nameof(Enumerable.Reverse):
                HandleReverse();
                break;
            case nameof(Enumerable.ElementAt):
                HandleElementAt(call);
                break;
            default:
                HandleContains(call);
                break;
        }
    }

    private bool RequiresWindowMaterialization(string name)
    {
        if (limit == null && offset == null)
        {
            return false;
        }

        return name switch
        {
            nameof(Enumerable.Where)
                or nameof(Enumerable.OrderBy) or nameof(Enumerable.OrderByDescending)
                or nameof(Enumerable.Distinct)
                or nameof(Enumerable.Skip)
                or nameof(Enumerable.ElementAt) => true,
            nameof(Enumerable.Take) => limit != null,
            _ => TranslationPatterns.IsWindowConsumer(name)
        };
    }

    private void MaterializeWindow()
    {
        string currentFrom = fromOverride ?? $"json_each({baseSource}){baseJoinSuffix}{crossJoin ?? ""}";

        List<string> clauses =
        [
            $"SELECT {selectExpr} AS \"value\", {keyColumn} AS \"key\"",
            $"FROM {currentFrom}"
        ];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        if (orderBys.Count > 0)
        {
            clauses.Add("ORDER BY " + string.Join(", ", orderBys));
        }

        clauses.Add(LimitOffsetClause()!);

        string wrapAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        fromOverride = $"({string.Join(" ", clauses)}) {wrapAlias}";
        baseJoinSuffix = "";
        crossJoin = null;
        wheres.Clear();
        orderBys.Clear();
        limit = null;
        offset = null;
        selectExpr = $"{wrapAlias}.\"value\"";
        keyColumn = $"{wrapAlias}.\"key\"";
    }

    private string? LimitOffsetClause()
    {
        if (limit != null && offset != null)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        if (limit != null)
        {
            return $"LIMIT {limit}";
        }

        if (offset != null)
        {
            return $"LIMIT -1 OFFSET {offset}";
        }

        return null;
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
        currentElementType = ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1])).ReturnType;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void HandleSelectMany(MethodCallExpression call, Type elementType)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1]);
        string outerAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        string selSql = VisitLambdaAliased(call.Arguments[1], elementType, outerAlias);
        string joinAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        baseJoinSuffix = $" {outerAlias}";
        crossJoin = $", json_each({selSql}) {joinAlias}";
        selectExpr = $"{joinAlias}.\"value\"";
        keyColumn = $"{joinAlias}.\"key\"";
        currentElementType = TypeHelpers.GetEnumerableElementType(lambda.ReturnType)!;
    }

    private void HandleSkip(MethodCallExpression call)
    {
        int n = Math.Max(0, (int)ExpressionHelpers.GetConstantValue(call.Arguments[1])!);
        offset = n.ToString(CultureInfo.InvariantCulture);
    }

    private void HandleTake(MethodCallExpression call)
    {
        int n = Math.Max(0, (int)ExpressionHelpers.GetConstantValue(call.Arguments[1])!);
        limit = n.ToString(CultureInfo.InvariantCulture);
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
        if (limit != null || offset != null)
        {
            bool hadOrder = orderBys.Count > 0;
            bool outerAscending = hadOrder && orderBys[^1].EndsWith(" DESC");
            MaterializeWindow();
            AddOptionalPredicate(call, elementType);
            orderBys.Add(hadOrder
                ? $"{selectExpr}{(outerAscending ? " ASC" : " DESC")}"
                : $"{keyColumn} DESC");
            limit = "1";
            wrapInArray = false;
            return;
        }

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
        selectExpr = distinct ? $"COUNT(DISTINCT {selectExpr})" : "COUNT(*)";
        distinct = false;
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
            : selectExpr;

        string aggregate = distinct ? $"{sqlFunc}(DISTINCT {inner})" : $"{sqlFunc}({inner})";
        selectExpr = sqlFunc == "SUM" ? $"COALESCE({aggregate}, 0)" : aggregate;
        distinct = false;
        wrapInArray = false;
    }

    private void HandleDistinct()
    {
        distinct = true;
        distinctSeenReverse = reverseApplied;
    }

    private void HandleReverse()
    {
        reverseApplied = true;

        if (limit != null || offset != null)
        {
            bool hadOrder = orderBys.Count > 0;
            bool lastWasAscending = hadOrder && orderBys[^1].EndsWith(" ASC");
            MaterializeWindow();
            orderBys.Add(hadOrder
                ? $"{selectExpr}{(lastWasAscending ? " DESC" : " ASC")}"
                : $"{keyColumn} DESC");
            return;
        }

        ReverseOrderBys();
    }

    private void ReverseOrderBys()
    {
        List<string> reversed = ReversedOrderBysList();
        orderBys.Clear();
        orderBys.AddRange(reversed);
    }

    private List<string> ReversedOrderBysList()
    {
        if (orderBys.Count == 0)
        {
            return [$"{keyColumn} DESC"];
        }

        List<string> reversed = new(orderBys.Count);
        foreach (string clause in orderBys)
        {
            reversed.Add(clause.EndsWith(" ASC")
                ? clause[..^4] + " DESC"
                : clause[..^5] + " ASC");
        }

        return reversed;
    }

    private void HandleElementAt(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        if (arg is { IsConstant: true, Constant: int index } && index < 0)
        {
            throw new ArgumentOutOfRangeException("index", index,
                $"{call.Method.Name} was called with a negative index ({index}). The index must be non-negative.");
        }

        offset = arg.SQLiteExpression!.ToString();
        AddParameters(arg);
        limit = "1";
        wrapInArray = false;
    }

    private void HandleContains(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        AddParameters(arg);
        wheres.Add($"{selectExpr} IS {arg.SQLiteExpression}");
        existsWrapper = "EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }
}
