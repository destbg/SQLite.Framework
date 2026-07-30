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
                HandleOrderBy(call, currentElementType, "ASC", primary: true);
                break;
            case nameof(Enumerable.OrderByDescending):
                HandleOrderBy(call, currentElementType, "DESC", primary: true);
                break;
            case nameof(Enumerable.ThenBy):
                HandleOrderBy(call, currentElementType, "ASC", primary: false);
                break;
            case nameof(Enumerable.ThenByDescending):
                HandleOrderBy(call, currentElementType, "DESC", primary: false);
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
            case nameof(Enumerable.LongCount):
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
        return fromOverride ?? $"json_each({baseSource}) {baseAlias}{crossJoin ?? ""}";
    }

    private void MaterializeWindow()
    {
        string currentFrom = CurrentFromClause();

        List<(string Expr, string Direction)> pendingOrder = SplitOrderBys();
        List<string> selectColumns = [$"{selectExpr} AS \"value\"", $"{keyColumn} AS \"key\""];
        bool carriesGroupKey = groupKeySql != null;
        if (carriesGroupKey)
        {
            selectColumns.Add($"{groupKeySql} AS \"grpkey\"");
        }

        for (int i = 0; i < pendingOrder.Count; i++)
        {
            selectColumns.Add($"{pendingOrder[i].Expr} AS \"o{i}\"");
        }

        List<string> clauses =
        [
            $"SELECT {string.Join(", ", selectColumns)}",
            $"FROM {currentFrom}"
        ];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        if (groupBys.Count > 0)
        {
            clauses.Add("GROUP BY " + string.Join(", ", groupBys));
        }

        if (havings.Count > 0)
        {
            clauses.Add("HAVING " + string.Join(" AND ", havings));
        }

        if (orderBys.Count > 0)
        {
            clauses.Add("ORDER BY " + string.Join(", ", orderBys));
        }

        clauses.Add(LimitOffsetClause()!);

        string wrapAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        fromOverride = $"({string.Join(" ", clauses)}) {wrapAlias}";
        crossJoin = null;
        wheres.Clear();
        groupBys.Clear();
        havings.Clear();
        orderBys.Clear();
        for (int i = 0; i < pendingOrder.Count; i++)
        {
            orderBys.Add($"{wrapAlias}.\"o{i}\" {pendingOrder[i].Direction}");
        }

        limit = null;
        offset = null;
        selectExpr = $"{wrapAlias}.\"value\"";
        keyColumn = $"{wrapAlias}.\"key\"";
        if (carriesGroupKey)
        {
            groupKeySql = $"{wrapAlias}.\"grpkey\"";
            groupElementSql = null;
            groupWindowMaterialized = true;
        }

        innerAliases.Clear();
        innerAliases.Add(wrapAlias);
    }

    private void MaterializeDistinct()
    {
        if (groupBys.Count > 0 || havings.Count > 0)
        {
            MaterializeWindow();
        }

        string currentFrom = CurrentFromClause();

        string keyAggregate = distinctSeenReverse ? "MAX" : "MIN";
        string keyDirection = reverseApplied ? " DESC" : "";

        List<(string Expr, string Direction)> pendingOrder = SplitOrderBys().Where(p => p.Expr != keyColumn).ToList();
        List<string> selectColumns = [$"{selectExpr} AS \"value\"", $"{keyAggregate}({keyColumn}) AS \"key\""];
        bool carriesGroupKey = groupKeySql != null;
        if (carriesGroupKey)
        {
            selectColumns.Add($"{keyAggregate}({groupKeySql}) AS \"grpkey\"");
        }

        List<string> groupOrder = [];
        for (int i = 0; i < pendingOrder.Count; i++)
        {
            string orderOperand = EnsureInnerReference(pendingOrder[i].Expr);
            string aggregated = pendingOrder[i].Direction == "DESC"
                ? $"MAX({orderOperand})"
                : $"MIN({orderOperand})";
            selectColumns.Add($"{aggregated} AS \"o{i}\"");
            groupOrder.Add($"{aggregated} {pendingOrder[i].Direction}");
        }

        List<string> clauses =
        [
            $"SELECT {string.Join(", ", selectColumns)}",
            $"FROM {currentFrom}"
        ];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        clauses.Add($"GROUP BY {selectExpr}");
        clauses.Add(groupOrder.Count > 0
            ? "ORDER BY " + string.Join(", ", groupOrder) + $", {keyAggregate}({keyColumn}){keyDirection}"
            : $"ORDER BY {keyAggregate}({keyColumn}){keyDirection}");

        string wrapAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        fromOverride = $"({string.Join(" ", clauses)}) {wrapAlias}";
        crossJoin = null;
        wheres.Clear();
        orderBys.Clear();
        for (int i = 0; i < pendingOrder.Count; i++)
        {
            orderBys.Add($"{wrapAlias}.\"o{i}\" {pendingOrder[i].Direction}");
        }

        orderBys.Add($"{wrapAlias}.\"key\" {(reverseApplied ? "DESC" : "ASC")}");

        distinct = false;
        distinctSeenReverse = false;
        reverseApplied = false;
        selectExpr = $"{wrapAlias}.\"value\"";
        keyColumn = $"{wrapAlias}.\"key\"";
        if (carriesGroupKey)
        {
            groupKeySql = $"{wrapAlias}.\"grpkey\"";
        }

        innerAliases.Clear();
        innerAliases.Add(wrapAlias);
    }

    private List<(string Expr, string Direction)> SplitOrderBys()
    {
        List<(string Expr, string Direction)> split = new(orderBys.Count);
        foreach (string clause in orderBys)
        {
            split.Add(clause.EndsWith(" ASC")
                ? (clause[..^4], "ASC")
                : (clause[..^5], "DESC"));
        }

        return split;
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
        if (groupBys.Count > 0)
        {
            havings.Add(predicate);
        }
        else
        {
            wheres.Add(predicate);
        }
    }

    private void HandleOrderBy(MethodCallExpression call, Type elementType, string direction, bool primary)
    {
        string keySql = VisitLambda(call.Arguments[1], elementType, coalesceLiftedComparison: true);
        Type keyType = ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1])).ReturnType;
        keyType = Nullable.GetUnderlyingType(keyType) ?? keyType;
        if (keyType.IsEnum && JsonEnumText.IsStringStored(options, keyType))
        {
            SQLiteExpression keyNumber = EnumMemberVisitor.BuildTextStorageEnumToNumber(
                visitor, typeof(long), keyType, SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), keySql));
            parameters.AddRange(keyNumber.Parameters!);
            keySql = keyNumber.ToString();
        }

        orderBys.Insert(primary ? 0 : primaryOrderCount, $"{keySql} {direction}");
        primaryOrderCount = primary ? 1 : primaryOrderCount + 1;
    }

    private string? TrailingPositionTiebreak()
    {
        if (orderBys.Count == 0)
        {
            return null;
        }

        (string expr, string _) = SplitOrderBys()[^1];
        return expr == keyColumn ? orderBys[^1] : null;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "IGrouping<,> is rooted by user code.")]
    private void HandleGroupBy(MethodCallExpression call, Type elementType)
    {
        if (distinct)
        {
            MaterializeDistinct();
        }

        if (groupBys.Count > 0 || groupWindowMaterialized)
        {
            MaterializeWindow();
            groupWindowMaterialized = false;
            if (!(elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>)))
            {
                groupKeySql = null;
                groupElementSql = null;
            }
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
        if (distinct)
        {
            MaterializeDistinct();
        }

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
            keyColumn = CompositePositionKey(keyColumn, overrideJoinAlias);
            innerAliases.Add(overrideJoinAlias);
            ApplySelectManyProjection(resultSelector, outerValueSql, elementType, $"{overrideJoinAlias}.\"value\"", innerElementType);
            return;
        }

        if (crossJoin != null)
        {
            string chainedOuterValueSql = selectExpr;
            string chainedInnerSql = VisitLambda(call.Arguments[1], elementType);
            string chainedJoinAlias = $"j{visitor.Counters.NextTableIndex('j')}";
            crossJoin = $"{crossJoin}, json_each({chainedInnerSql}) {chainedJoinAlias}";
            keyColumn = CompositePositionKey(keyColumn, chainedJoinAlias);
            innerAliases.Add(chainedJoinAlias);
            ApplySelectManyProjection(resultSelector, chainedOuterValueSql, elementType, $"{chainedJoinAlias}.\"value\"", innerElementType);
            return;
        }

        string outerValue = selectExpr;
        string selSql = VisitLambda(call.Arguments[1], elementType);
        string joinAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        crossJoin = $", json_each({selSql}) {joinAlias}";
        keyColumn = CompositePositionKey(keyColumn, joinAlias);
        innerAliases.Add(joinAlias);
        ApplySelectManyProjection(resultSelector, outerValue, elementType, $"{joinAlias}.\"value\"", innerElementType);
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
        if (distinct)
        {
            MaterializeDistinct();
        }

        offset = ResolveCountArgument(call.Arguments[1]);
    }

    private void HandleTake(MethodCallExpression call)
    {
        if (distinct)
        {
            MaterializeDistinct();
        }

        limit = ResolveCountArgument(call.Arguments[1]);
    }

    private void AddOptionalPredicate(MethodCallExpression call, Type elementType)
    {
        if (call.Arguments.Count > 1)
        {
            string predicate = VisitLambda(call.Arguments[1], elementType);
            if (groupBys.Count > 0)
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
        SelectGroupKeyForGroupingResult();
        SelectEntryObjectForDictionaryResult();
        limit = "1";
        wrapInArray = false;
    }

    private void SelectGroupKeyForGroupingResult()
    {
        if (groupBys.Count > 0 && groupKeySql != null
            && currentElementType is { IsGenericType: true }
            && currentElementType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            selectExpr = groupKeySql;
        }
    }

    private void SelectEntryObjectForDictionaryResult()
    {
        if (currentElementType is { IsGenericType: true }
            && currentElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            selectExpr = $"json_object('Key', {keyColumn}, 'Value', {selectExpr})";
        }
    }

    private void HandleLast(MethodCallExpression call, Type elementType)
    {
        if (limit != null || offset != null)
        {
            MaterializeWindow();
        }

        AddOptionalPredicate(call, elementType);
        SelectGroupKeyForGroupingResult();
        SelectEntryObjectForDictionaryResult();
        ReverseOrderWithPosition();

        limit = "1";
        wrapInArray = false;
    }

    private void HandleSingle(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        SelectGroupKeyForGroupingResult();
        SelectEntryObjectForDictionaryResult();
        singleSemantic = true;
        wrapInArray = false;
    }

    private void HandleCount(MethodCallExpression call, Type elementType)
    {
        AddOptionalPredicate(call, elementType);
        if (distinct && groupBys.Count > 0)
        {
            MaterializeDistinct();
        }

        if (groupBys.Count > 0)
        {
            countsGroups = true;
            selectExpr = "COUNT(*)";
        }
        else
        {
            selectExpr = distinct ? $"COUNT(DISTINCT {EnsureInnerReference(selectExpr)})" : "COUNT(*)";
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
        if (groupBys.Count > 0)
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

        if (groupBys.Count > 0)
        {
            selectExpr = inner;
            MaterializeWindow();
            inner = selectExpr;
        }

        Type valueType = hasSelector
            ? ((LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[1])).ReturnType
            : elementType;
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        bool stringEnumAggregate = sqlFunc is "MIN" or "MAX"
            && valueType.IsEnum
            && JsonEnumText.IsStringStored(options, valueType);
        if (stringEnumAggregate)
        {
            SQLiteExpression innerNumber = EnumMemberVisitor.BuildTextStorageEnumToNumber(
                visitor, typeof(long), valueType, SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), inner),
                inlineOperand: true);
            parameters.AddRange(innerNumber.Parameters!);
            inner = innerNumber.ToString();
        }

        inner = EnsureInnerReference(inner);
        string aggregate = distinct ? $"{sqlFunc}(DISTINCT {inner})" : $"{sqlFunc}({inner})";
        selectExpr = sqlFunc == "SUM" ? $"COALESCE({aggregate}, 0)" : aggregate;
        if (stringEnumAggregate)
        {
            stringEnumNameWrapType = valueType;
        }

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
        reverseApplied = !reverseApplied;

        if (limit != null || offset != null)
        {
            MaterializeWindow();
        }

        ReverseOrderWithPosition();
    }

    private void ReverseOrderWithPosition()
    {
        bool hadOrderBys = orderBys.Count > 0;
        bool ordersByPosition = TrailingPositionTiebreak() != null;
        ReverseOrderBys();
        if (hadOrderBys && !ordersByPosition)
        {
            orderBys.Add($"{keyColumn} DESC");
        }
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

        if (distinct)
        {
            MaterializeDistinct();
        }

        SelectGroupKeyForGroupingResult();
        SelectEntryObjectForDictionaryResult();
        offset = arg.SQLiteExpression!.ToString();
        AddParameters(arg);
        limit = "1";
        wrapInArray = false;
    }

    private void HandleContains(MethodCallExpression call)
    {
        ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
        (string matchSql, SQLiteParameter[]? matchParameters) = JsonMethodTranslator.ResolveElementMatchArgument(visitor, arg);
        if (matchParameters != null)
        {
            parameters.AddRange(matchParameters);
        }

        List<string> containsSink = groupBys.Count > 0 ? havings : wheres;
        containsSink.Add($"{selectExpr} IS {matchSql}");
        existsWrapper = "EXISTS";
        selectExpr = "1";
        limit = "1";
        wrapInArray = false;
    }

    private static string CompositePositionKey(string outerKey, string joinAlias)
    {
        return $"({outerKey} * 1000000000 + {joinAlias}.\"key\")";
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
