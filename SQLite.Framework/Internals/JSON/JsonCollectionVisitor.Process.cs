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
                or nameof(Enumerable.GroupBy)
                or nameof(Enumerable.Skip)
                or nameof(Enumerable.ElementAt)
                or nameof(Enumerable.SelectMany)
                or nameof(Enumerable.First) or nameof(Enumerable.FirstOrDefault)
                or nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault) => true,
            nameof(Enumerable.Take) => limit != null,
            _ => TranslationPatterns.IsWindowConsumer(name)
        };
    }

    private string CurrentFromClause()
    {
        return fromOverride ?? $"json_each({baseSource}){baseJoinSuffix}{crossJoin ?? ""}";
    }

    private void MaterializeWindow()
    {
        string currentFrom = CurrentFromClause();

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
        havings.Clear();
        orderBys.Clear();
        limit = null;
        offset = null;
        selectExpr = $"{wrapAlias}.\"value\"";
        keyColumn = $"{wrapAlias}.\"key\"";
    }

    private void MaterializeDistinct()
    {
        string currentFrom = CurrentFromClause();

        List<string> clauses =
        [
            $"SELECT {selectExpr} AS \"value\", MIN({keyColumn}) AS \"key\"",
            $"FROM {currentFrom}"
        ];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        clauses.Add($"GROUP BY {selectExpr}");
        clauses.Add($"ORDER BY MIN({keyColumn})");

        string wrapAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        fromOverride = $"({string.Join(" ", clauses)}) {wrapAlias}";
        baseJoinSuffix = "";
        crossJoin = null;
        wheres.Clear();
        orderBys.Clear();
        distinct = false;
        distinctSeenReverse = false;
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
        string predicate = VisitLambda(call.Arguments[1], elementType);
        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            havings.Add(predicate);
        }
        else
        {
            wheres.Add(predicate);
        }
    }

    private void HandleOrderBy(MethodCallExpression call, Type elementType, string direction, bool clear)
    {
        if (clear)
        {
            orderBys.Clear();
        }

        orderBys.Add($"{VisitLambda(call.Arguments[1], elementType, coalesceLiftedComparison: true)} {direction}");
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "IGrouping<,> is rooted by user code.")]
    private void HandleGroupBy(MethodCallExpression call, Type elementType)
    {
        if (distinct)
        {
            MaterializeDistinct();
        }

        string keySql = VisitLambda(call.Arguments[1], elementType, coalesceLiftedComparison: true);
        groupBys.Add(keySql);
        groupKeySql = keySql;

        Type keyType = ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1])).ReturnType;
        Type groupElementType = elementType;
        if (call.Arguments.Count == 3)
        {
            groupElementSql = VisitLambda(call.Arguments[2], elementType);
            groupElementType = ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[2])).ReturnType;
        }
        else
        {
            groupElementSql = selectExpr;
        }

        currentElementType = typeof(IGrouping<,>).MakeGenericType(keyType, groupElementType);
    }

    private void HandleSelect(MethodCallExpression call, Type elementType)
    {
        if (distinct)
        {
            MaterializeDistinct();
        }

        selectExpr = VisitLambda(call.Arguments[1], elementType);
        currentElementType = ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1])).ReturnType;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void HandleSelectMany(MethodCallExpression call, Type elementType)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1]);
        Type innerElementType = TypeHelpers.GetEnumerableElementType(lambda.ReturnType)!;
        LambdaExpression? resultSelector = call.Arguments.Count == 3
            ? (LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[2])
            : null;

        if (fromOverride != null)
        {
            string outerValueSql = selectExpr;
            string innerSql = VisitLambda(call.Arguments[1], elementType);
            string overrideJoinAlias = $"j{visitor.Counters.NextTableIndex('j')}";
            fromOverride = $"{fromOverride}, json_each({innerSql}) {overrideJoinAlias}";
            keyColumn = $"{overrideJoinAlias}.\"key\"";
            ApplySelectManyProjection(resultSelector, outerValueSql, elementType, $"{overrideJoinAlias}.\"value\"", innerElementType);
            return;
        }

        string outerAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        string selSql = VisitLambdaAliased(call.Arguments[1], elementType, outerAlias);
        string joinAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        baseJoinSuffix = $" {outerAlias}";
        crossJoin = $", json_each({selSql}) {joinAlias}";
        keyColumn = $"{joinAlias}.\"key\"";
        ApplySelectManyProjection(resultSelector, $"{outerAlias}.\"value\"", elementType, $"{joinAlias}.\"value\"", innerElementType);
    }

    private void ApplySelectManyProjection(LambdaExpression? resultSelector, string outerValueSql, Type outerElementType, string innerValueSql, Type innerElementType)
    {
        if (resultSelector == null)
        {
            selectExpr = innerValueSql;
            currentElementType = innerElementType;
            return;
        }

        BindParameter(resultSelector.Parameters[0], outerElementType, outerValueSql);
        BindParameter(resultSelector.Parameters[1], innerElementType, innerValueSql);
        selectExpr = TranslateBody(resultSelector.Body);
        visitor.MethodArguments.Remove(resultSelector.Parameters[0]);
        visitor.MethodArguments.Remove(resultSelector.Parameters[1]);
        currentElementType = resultSelector.ReturnType;
    }

    private void HandleSkip(MethodCallExpression call)
    {
        offset = ResolveCountArgument(call.Arguments[1]);
    }

    private void HandleTake(MethodCallExpression call)
    {
        limit = ResolveCountArgument(call.Arguments[1]);
    }

    private void AddOptionalPredicate(MethodCallExpression call, Type elementType)
    {
        if (call.Arguments.Count > 1)
        {
            string predicate = VisitLambda(call.Arguments[1], elementType);
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                havings.Add(predicate);
            }
            else
            {
                wheres.Add(predicate);
            }
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
            List<string>? reversedOrder = orderBys.Count > 0 ? ReversedOrderBysList() : null;
            MaterializeWindow();
            AddOptionalPredicate(call, elementType);
            if (reversedOrder != null)
            {
                orderBys.AddRange(reversedOrder);
            }
            else
            {
                orderBys.Add($"{keyColumn} DESC");
            }
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
        if (groupBys.Count > 0)
        {
            countsGroups = true;
            selectExpr = "COUNT(*)";
        }
        else
        {
            selectExpr = distinct ? $"COUNT(DISTINCT {selectExpr})" : "COUNT(*)";
        }

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
        string predicate = $"(({VisitLambda(call.Arguments[1], elementType)}) IS NOT 1)";
        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            havings.Add(predicate);
        }
        else
        {
            wheres.Add(predicate);
        }

        existsWrapper = "NOT EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }

    private void HandleAggregate(MethodCallExpression call, Type elementType, string sqlFunc)
    {
        bool hasSelector = call.Arguments.Count > 1;

        if (distinct && hasSelector && sqlFunc is "SUM" or "AVG")
        {
            MaterializeDistinct();
        }

        string inner = hasSelector
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
            List<string>? reversedOrder = orderBys.Count > 0 ? ReversedOrderBysList() : null;
            MaterializeWindow();
            if (reversedOrder != null)
            {
                orderBys.AddRange(reversedOrder);
            }
            else
            {
                orderBys.Add($"{keyColumn} DESC");
            }
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

    private static string ResolveCountArgument(Expression arg)
    {
        if (ExpressionHelpers.IsConstant(arg) && ExpressionHelpers.GetConstantValue(arg) is int n)
        {
            return Math.Max(0, n).ToString(CultureInfo.InvariantCulture);
        }

        throw new NotSupportedException(
            "Skip and Take on a JSON array support a constant or captured value, not a column of the outer row.");
    }
}
