namespace SQLite.Framework.Internals.Visitors.Member;

internal static class QueryableMemberVisitor
{
    public static Expression HandleQueryableMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        SQLTranslator translator = visitor.CloneDeeper(visitor.Level + 1);
        SQLQuery query = translator.Translate(node);

        string querySql = query.Sql;
        SQLiteParameter[]? queryParams = query.Parameters.Count != 0
            ? query.Parameters.ToArray()
            : null;

        if (node.Method.Name == nameof(System.Linq.Queryable.Any))
        {
            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"EXISTS ({Environment.NewLine}{querySql}{Environment.NewLine})", queryParams);
        }

        if (node.Method.Name == nameof(System.Linq.Queryable.All))
        {
            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"NOT EXISTS ({Environment.NewLine}{querySql}{Environment.NewLine})", queryParams);
        }

        if (node.Arguments.Count == 1 || node.Method.Name != nameof(System.Linq.Queryable.Contains))
        {
            if (translator.Selects.Count > 1)
            {
                throw new NotSupportedException(
                    $"{node.Method.Name} returns an entity-typed scalar subquery, which is not supported " +
                    "because a scalar subquery can return only one column. " +
                    "Project the column inside the subquery first, for example '.Select(x => x.Column).FirstOrDefault()'.");
            }

            SQLiteExpression scalarSubquery = SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"({Environment.NewLine}{querySql}{Environment.NewLine})", queryParams);
            if (translator.Selects.Count == 1)
            {
                if (translator.Selects[0].IsDayOfWeekInteger)
                {
                    scalarSubquery.WithDayOfWeekInteger();
                }

                if (translator.Selects[0].IsJsonSource)
                {
                    scalarSubquery.WithJsonSource();
                }
            }

            return scalarSubquery;
        }

        List<ResolvedModel> arguments = node.Arguments
            .Skip(1)
            .Select(visitor.ResolveExpression)
            .ToList();

        SQLiteExpression firstArg = arguments[0].SQLiteExpression!;
        SQLiteParameter[]? argParams = ParameterHelpers.CombineParameters([firstArg, .. arguments.Skip(1).Select(f => f.SQLiteExpression!)]);
        SQLiteParameter[]? parameters = queryParams == null
            ? argParams
            : argParams == null ? queryParams : [.. queryParams, .. argParams];
        string containsColumn = ContainsColumnName(translator);
        Type containsItemType = Nullable.GetUnderlyingType(node.Arguments[1].Type) ?? node.Arguments[1].Type;
        if (firstArg.IsDayOfWeekInteger
            && visitor.Database.Options.EnumStorage == EnumStorageMode.Text
            && containsItemType == typeof(DayOfWeek))
        {
            SQLiteExpression columnLeaf = SQLiteExpression.Leaf(typeof(DayOfWeek), visitor.Counters.NextIdentifier(), $"\"{containsColumn}\"");
            SQLiteExpression columnNumber = EnumMemberVisitor.BuildTextStorageEnumToNumber(visitor, typeof(int), typeof(DayOfWeek), columnLeaf);
            SQLiteParameter[] caseParameters = ParameterHelpers.CombineParameters(columnNumber, firstArg)!;
            SQLiteParameter[] containsParameters = queryParams == null
                ? caseParameters
                : [.. queryParams, .. caseParameters];
            return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                [$"EXISTS ({Environment.NewLine}SELECT 1 FROM ({Environment.NewLine}{querySql}{Environment.NewLine}) WHERE ", " IS ", ")"],
                [columnNumber, firstArg], containsParameters);
        }

        if (!firstArg.IsDayOfWeekInteger
            && visitor.Database.Options.EnumStorage == EnumStorageMode.Text
            && containsItemType == typeof(DayOfWeek)
            && ContainsSourceIsDayOfWeekInteger(translator))
        {
            SQLiteExpression itemNumber = EnumMemberVisitor.BuildTextStorageEnumToNumber(visitor, typeof(int), typeof(DayOfWeek), firstArg);
            SQLiteParameter[]? itemParameters = queryParams == null
                ? itemNumber.Parameters
                : [.. queryParams, .. itemNumber.Parameters!];
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                $"EXISTS ({Environment.NewLine}SELECT 1 FROM ({Environment.NewLine}{querySql}{Environment.NewLine}) WHERE \"{containsColumn}\" IS ", itemNumber, ")", itemParameters);
        }

        return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
            $"EXISTS ({Environment.NewLine}SELECT 1 FROM ({Environment.NewLine}{querySql}{Environment.NewLine}) WHERE \"{containsColumn}\" IS ", firstArg, ")", parameters);
    }

    public static bool IsSystemMethod(MethodInfo method)
    {
        return method.DeclaringType?.Namespace is { } ns
            && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal));
    }

    public static Expression HandleEnumerableMethod(SQLVisitor visitor, MethodCallExpression node, IEnumerable enumerable, List<ResolvedModel> arguments)
    {
        ComparerArgumentGuard.ThrowIfComparer(node);

        int firstItemArgIndex = node.Object == null ? 1 : 0;

        if (arguments.Skip(firstItemArgIndex).Any(f => f.SQLiteExpression == null))
        {
            return Expression.Call(node.Object, node.Method, node.Arguments.Select((argument, i) => visitor.ToClientOperand(argument, arguments[i])));
        }

        if (node.Object == null
            && TypeHelpers.IsSimple(node.Method.ReturnType, visitor.Database.Options)
            && arguments.Skip(firstItemArgIndex).All(f => f.IsConstant))
        {
            if (visitor.ClientEvalAllowed && !IsSystemMethod(node.Method))
            {
                return Expression.Call(node.Object, node.Method, node.Arguments.Select((argument, i) => visitor.ToClientOperand(argument, arguments[i])));
            }

            ParameterInfo[] methodParameters = node.Method.GetParameters();
            object? result;
            if (methodParameters.Length > 0 && methodParameters[0].ParameterType.IsByRefLike)
            {
                result = ExpressionHelpers.GetConstantValue(node);
            }
            else
            {
                try
                {
                    result = node.Method.Invoke(null, [
                        enumerable,
                        ..node.Arguments.Skip(1).Select(ExpressionHelpers.GetConstantValue)
                    ]);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            string pName = visitor.Counters.NextParamName();

            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
                int itemIndex = node.Object == null ? 1 : 0;
                return BuildEnumerableContains(visitor, node, enumerable, itemIndex, arguments[itemIndex]);
        }

        return Expression.Call(node.Object, node.Method, node.Arguments.Select((argument, i) => visitor.ToClientOperand(argument, arguments[i])));
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Builds an expression tree for the translator.")]
    public static Expression HandleGroupingMethod(SQLVisitor visitor, MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Enumerable.Contains) && node.Arguments.Count == 2)
        {
            Type containsElementType = node.Method.GetGenericArguments()[0];
            ParameterExpression element = Expression.Parameter(containsElementType, "e");
            LambdaExpression predicate = Expression.Lambda(Expression.Equal(element, node.Arguments[1]), element);
            node = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [containsElementType], node.Arguments[0], predicate);
        }
        else if (node.Arguments.Count >= 2 && ExpressionHelpers.StripQuotes(node.Arguments[1]) is not LambdaExpression)
        {
            throw new NotSupportedException($"Grouping aggregate {node.Method.Name} is not translatable to SQL.");
        }

        Expression receiver = node.Arguments[0];
        LambdaExpression? whereFilter = TryPeelWhereFilter(ref receiver);

        bool isCount = node.Method.Name is nameof(Enumerable.Count) or nameof(Enumerable.LongCount);
        LambdaExpression? countPredicate = isCount && node.Arguments.Count == 2
            ? (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1])
            : null;

        List<LambdaExpression> filterLambdas = [];
        if (whereFilter != null)
        {
            filterLambdas.Add(whereFilter);
        }

        if (countPredicate != null)
        {
            filterLambdas.Add(countPredicate);
        }

        Dictionary<string, Expression>? newTableColumns = null;
        SQLiteExpression? sqlExpression = null;

        if (node.Arguments.Count == 2 || filterLambdas.Count > 0)
        {
            newTableColumns = BuildGroupingColumnMap(visitor, receiver);
        }

        SQLiteExpression? filterExpression = null;
        foreach (LambdaExpression filter in filterLambdas)
        {
            visitor.MethodArguments[filter.Parameters[0]] = newTableColumns!;
            Expression resolvedFilter = visitor.Visit(filter.Body);
            if (resolvedFilter is not SQLiteExpression sqlFilter)
            {
                throw new NotSupportedException("Aggregate FILTER predicate could not be resolved.");
            }

            filterExpression = filterExpression == null
                ? sqlFilter
                : SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", filterExpression, " AND ", sqlFilter, ")", ParameterHelpers.CombineParameters(filterExpression, sqlFilter));
        }

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (filterExpression != null)
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "FILTER (WHERE ...) on aggregates");
        }
#endif

        switch (node.Method.Name)
        {
            case nameof(Enumerable.LongCount):
            case nameof(Enumerable.Count):
                return BuildCountExpression(visitor, node, filterExpression);
        }

        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            visitor.MethodArguments[lambda.Parameters[0]] = newTableColumns!;
        }
        else
        {
            Expression expression = visitor.ResolveMember(receiver);

            if (expression is not SQLiteExpression expr)
            {
                throw new NotSupportedException("Grouping key could not be resolved.");
            }

            sqlExpression = expr;
        }

        return node.Method.Name switch
        {
            nameof(Enumerable.Sum) => AggregateExpression(visitor, node, "SUM", sqlExpression, filterExpression),
            nameof(Enumerable.Average) => AggregateExpression(visitor, node, "AVG", sqlExpression, filterExpression),
            nameof(Enumerable.Min) => AggregateExpression(visitor, node, "MIN", sqlExpression, filterExpression),
            nameof(Enumerable.Max) => AggregateExpression(visitor, node, "MAX", sqlExpression, filterExpression),
            nameof(Enumerable.Any) => QuantifierExpression(visitor, node, "MAX", filterExpression),
            nameof(Enumerable.All) => QuantifierExpression(visitor, node, "MIN", filterExpression),
            _ => throw new NotSupportedException($"Grouping aggregate {node.Method.Name} is not translatable to SQL.")
        };
    }

    public static Expression? TryHandleGroupingConcat(SQLVisitor visitor, MethodCallExpression node)
    {
        bool isJoin = node.Method.Name == nameof(string.Join);
        if ((isJoin && node.Arguments.Count != 2) || (!isJoin && node.Arguments.Count != 1))
        {
            return null;
        }

        Expression source = node.Arguments[isJoin ? 1 : 0];
        if (!IsGroupingRooted(source))
        {
            return null;
        }

        Expression receiver = source;
        bool distinctElements = TryPeelDistinct(ref receiver);
        LambdaExpression? selector = TryPeelSelectSelector(ref receiver);
        LambdaExpression? whereFilter = TryPeelWhereFilter(ref receiver);
        if (receiver is MethodCallExpression)
        {
            return null;
        }

        if (distinctElements && !IsCommaSeparatorJoin(node, isJoin))
        {
            return null;
        }

        Dictionary<string, Expression> columns = BuildGroupingColumnMap(visitor, receiver);

        SQLiteExpression? filterExpression = null;
        if (whereFilter != null)
        {
            visitor.MethodArguments[whereFilter.Parameters[0]] = columns;
            if (visitor.Visit(whereFilter.Body) is not SQLiteExpression sqlFilter)
            {
                throw new NotSupportedException($"string.{node.Method.Name} could not resolve the group filter.");
            }

            filterExpression = sqlFilter;
        }

        SQLiteExpression target;
        if (selector != null)
        {
            visitor.MethodArguments[selector.Parameters[0]] = columns;
            if (visitor.Visit(selector.Body) is not SQLiteExpression sqlTarget)
            {
                throw new NotSupportedException($"string.{node.Method.Name} could not resolve the element selector.");
            }

            target = sqlTarget;
        }
        else if (columns.TryGetValue(string.Empty, out Expression? element) && element is SQLiteExpression sqlElement)
        {
            target = sqlElement;
        }
        else
        {
            throw new NotSupportedException($"string.{node.Method.Name} over a group of rows is not supported.");
        }

        SQLiteExpression separator;
        if (isJoin)
        {
            Expression separatorArg = node.Arguments[0];
            if (separatorArg.Type == typeof(char))
            {
                if (!ExpressionHelpers.IsConstant(separatorArg))
                {
                    throw new NotSupportedException(
                        "string.Join with a char separator that is not a constant is not supported. Use a constant separator.");
                }

                separatorArg = Expression.Constant(((char)ExpressionHelpers.GetConstantValue(separatorArg)!).ToString());
            }

            if (visitor.ResolveExpression(separatorArg).SQLiteExpression is not { } sqlSeparator)
            {
                throw new NotSupportedException("string.Join could not resolve the separator.");
            }

            separator = sqlSeparator;
        }
        else
        {
            separator = SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), "''");
        }

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (filterExpression != null)
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "FILTER (WHERE ...) on aggregates");
        }
#endif

        if (distinctElements)
        {
            if (filterExpression == null)
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                    "COALESCE(group_concat(DISTINCT COALESCE(", target, ", '')), '')", target.Parameters);
            }

            return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                "COALESCE(group_concat(DISTINCT COALESCE(", target, ", '')) FILTER (WHERE ", filterExpression, "), '')",
                ParameterHelpers.CombineParameters(target, filterExpression));
        }

        if (filterExpression == null)
        {
            return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                ["COALESCE(group_concat(COALESCE(", ", ''), ", "), '')"],
                [target, separator],
                ParameterHelpers.CombineParameters(target, separator));
        }

        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
            ["COALESCE(group_concat(COALESCE(", ", ''), ", ") FILTER (WHERE ", "), '')"],
            [target, separator, filterExpression],
            ParameterHelpers.CombineParameters(target, separator, filterExpression));
    }

    public static bool IsGroupingRooted(Expression source)
    {
        Expression current = source;
        while (true)
        {
            if (current.Type.IsGenericType && current.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                return true;
            }

            if (current is MethodCallExpression { Object: null, Arguments.Count: > 0 } call)
            {
                current = call.Arguments[0];
                continue;
            }

            return false;
        }
    }

    public static LambdaExpression? TryPeelSelectSelector(ref Expression receiver)
    {
        if (receiver is not MethodCallExpression selectCall
            || selectCall.Method.Name != nameof(Enumerable.Select)
            || selectCall.Arguments.Count != 2)
        {
            return null;
        }

        LambdaExpression candidate = (LambdaExpression)ExpressionHelpers.StripQuotes(selectCall.Arguments[1]);
        if (candidate.Parameters.Count != 1)
        {
            return null;
        }

        receiver = selectCall.Arguments[0];
        return candidate;
    }

    public static LambdaExpression? TryPeelWhereFilter(ref Expression receiver)
    {
        if (receiver is not MethodCallExpression whereCall)
        {
            return null;
        }

        if (whereCall.Method.Name != nameof(Enumerable.Where))
        {
            return null;
        }

        LambdaExpression candidate = (LambdaExpression)ExpressionHelpers.StripQuotes(whereCall.Arguments[1]);
        if (candidate.Parameters.Count != 1)
        {
            return null;
        }

        receiver = whereCall.Arguments[0];
        return candidate;
    }

    public static Dictionary<string, Expression> BuildGroupingColumnMap(SQLVisitor visitor, Expression receiver)
    {
        (string path, ParameterExpression pe) = ExpressionHelpers.ResolveParameterPath(receiver);

        Dictionary<string, Expression> newTableColumns = [];
        Dictionary<string, Expression> sourceColumns = visitor.MethodArguments[pe];

        if (visitor.OptionalRowPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceOptionalPaths))
        {
            HashSet<string> strippedOptionalPaths = new(StringComparer.Ordinal);
            foreach (string optionalPath in sourceOptionalPaths)
            {
                strippedOptionalPaths.Add(optionalPath[Constants.GroupingElementPrefix.Length..]);
            }

            visitor.OptionalRowPaths[newTableColumns] = strippedOptionalPaths;
        }

        if (visitor.ConstructedProjectionPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceConstructedPaths))
        {
            HashSet<string> strippedConstructedPaths = new(StringComparer.Ordinal);
            foreach (string constructedPath in sourceConstructedPaths)
            {
                strippedConstructedPaths.Add(constructedPath.StartsWith(Constants.GroupingElementPrefix, StringComparison.Ordinal)
                    ? constructedPath[Constants.GroupingElementPrefix.Length..]
                    : constructedPath);
            }

            visitor.ConstructedProjectionPaths[newTableColumns] = strippedConstructedPaths;
        }

        foreach (KeyValuePair<string, Expression> kvp in sourceColumns)
        {
            if (kvp.Key.StartsWith(Constants.GroupingElementPrefix, StringComparison.Ordinal))
            {
                newTableColumns[kvp.Key[Constants.GroupingElementPrefix.Length..]] = kvp.Value;
                continue;
            }

            if (kvp.Key == nameof(IGrouping<,>.Key))
            {
                continue;
            }

            if (kvp.Key.StartsWith(path))
            {
                int length = path.Length + nameof(IGrouping<,>.Key).Length + 1;
                string[] split = kvp.Key[Math.Min(length, kvp.Key.Length)..]
                    .Split('.', StringSplitOptions.RemoveEmptyEntries);

                string newKey = string.Join('.', split);

                if (!newTableColumns.ContainsKey(newKey))
                {
                    newTableColumns[newKey] = kvp.Value;
                }
            }
        }

        return newTableColumns;
    }

    public static bool CheckConstantMethod<T>(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, [MaybeNullWhen(false)] out Expression expression)
    {
        if (arguments.Any(f => f.SQLiteExpression == null))
        {
            ParameterInfo[] parameters = node.Method.GetParameters();
            IEnumerable<Expression> callArguments = arguments.Select((f, i) =>
                f.Expression.Type != parameters[i].ParameterType
                    ? Expression.Convert(f.Expression, parameters[i].ParameterType)
                    : f.Expression);

            expression = Expression.Call(node.Method, callArguments);
            return true;
        }

        Type type = typeof(T);

        if (node.Object == null && node.Method.ReturnType.IsAssignableTo(type) && arguments.All(f => f.IsConstant))
        {
            object? result;
            try
            {
                result = node.Method.Invoke(null, arguments.Select(f => f.Constant).ToArray());
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }

            string pName = visitor.Counters.NextParamName();
            expression = SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
            return true;
        }

        expression = null;
        return false;
    }

    public static Expression? TryHandleLocalCollectionOperator(SQLVisitor visitor, MethodCallExpression node)
    {
        bool isSpanContains = node.Method.DeclaringType == typeof(MemoryExtensions)
            && node.Method.Name == nameof(Enumerable.Contains)
            && node.Arguments.Count == 2;
        if (node.Method.DeclaringType != typeof(Enumerable) && !isSpanContains)
        {
            return null;
        }

        bool isAny = node.Method.Name == nameof(Enumerable.Any) && node.Arguments.Count == 2;
        bool isCount = (node.Method.Name is nameof(Enumerable.Count) or nameof(Enumerable.LongCount))
            && node.Arguments.Count == 2;
        bool isContains = node.Method.Name == nameof(Enumerable.Contains) && node.Arguments.Count == 2;
        if (!isAny && !isCount && !isContains)
        {
            return null;
        }

        LambdaExpression? terminalLambda = null;
        if (!isContains)
        {
            terminalLambda = ExpressionHelpers.StripQuotes(node.Arguments[1]) as LambdaExpression;
            if (terminalLambda == null)
            {
                return null;
            }
        }

        Expression source = isSpanContains
            ? StripSpanConversion(node.Arguments[0])
            : node.Arguments[0];
        List<LambdaExpression> sourceFilters = [];
        while (source is MethodCallExpression whereCall
            && whereCall.Method.DeclaringType == typeof(Enumerable)
            && whereCall.Method.Name == nameof(Enumerable.Where)
            && whereCall.Arguments.Count == 2
            && ExpressionHelpers.StripQuotes(whereCall.Arguments[1]) is LambdaExpression { Parameters.Count: 1 } whereLambda)
        {
            sourceFilters.Add(whereLambda);
            source = whereCall.Arguments[0];
        }

        if (!IsEvaluableLocalCollectionSource(source))
        {
            if (isContains && source is NewArrayExpression { NodeType: ExpressionType.NewArrayBounds })
            {
                return visitor.NotTranslatable(node, "Contains over an array created with a row-dependent length is not translatable to SQL.");
            }

            return isContains && sourceFilters.Count == 0 && source is NewArrayExpression { NodeType: ExpressionType.NewArrayInit } inlineArray
                ? BuildInlineArrayContains(visitor, node, inlineArray)
                : null;
        }

        object? sourceValue = ExpressionHelpers.GetConstantValue(source);
        if (sourceValue == null && !isSpanContains)
        {
            throw new ArgumentNullException(nameof(source), "The local collection used by Contains cannot be null.");
        }

        IEnumerable enumerable = sourceValue == null
            ? Array.Empty<object?>()
            : (IEnumerable)sourceValue;

        if (isContains && sourceFilters.Count == 0)
        {
            ResolvedModel item = visitor.ResolveExpression(node.Arguments[1]);
            return item.SQLiteExpression == null
                ? null
                : BuildEnumerableContains(visitor, node, enumerable, 1, item);
        }

        Type elementType = node.Method.GetGenericArguments()[0];
        ParameterExpression element = isContains
            ? Expression.Parameter(elementType, "local")
            : terminalLambda!.Parameters[0];
        Expression predicate = isContains
            ? Expression.Equal(element, node.Arguments[1])
            : terminalLambda!.Body;

        for (int i = sourceFilters.Count - 1; i >= 0; i--)
        {
            LambdaExpression filter = sourceFilters[i];
            Expression filterBody = new ParameterSubstitutor(filter.Parameters[0], element).Visit(filter.Body);
            predicate = Expression.AndAlso(filterBody, predicate);
        }

        List<object?> items = enumerable.Cast<object?>().ToList();
        return BuildLocalCollectionExpression(visitor, node.Method.ReturnType, element, predicate, items, isAny || isContains);
    }

    public static bool IsEvaluableLocalCollectionSource(Expression source)
    {
        if (ExpressionHelpers.IsConstant(source)
            || source is MethodCallExpression methodCall && ExpressionHelpers.IsConstantMethodCall(methodCall))
        {
            return true;
        }

        return source is NewArrayExpression { NodeType: ExpressionType.NewArrayInit } array
            && array.Expressions.All(IsSafeLocalCollectionValue);
    }

    private static bool ContainsSourceIsDayOfWeekInteger(SQLTranslator translator)
    {
        if (translator.Selects.Count > 0)
        {
            return translator.Selects[0].IsDayOfWeekInteger;
        }

        return ((SQLiteExpression)translator.Visitor.TableColumns.Values.First()).IsDayOfWeekInteger;
    }

    private static bool TryPeelDistinct(ref Expression receiver)
    {
        if (receiver is MethodCallExpression { Method.Name: nameof(Enumerable.Distinct), Arguments.Count: 1 } distinctCall)
        {
            receiver = distinctCall.Arguments[0];
            return true;
        }

        return false;
    }

    private static bool IsCommaSeparatorJoin(MethodCallExpression node, bool isJoin)
    {
        if (!isJoin)
        {
            return false;
        }

        Expression separatorArg = node.Arguments[0];
        if (!ExpressionHelpers.IsConstant(separatorArg))
        {
            return false;
        }

        object? separator = ExpressionHelpers.GetConstantValue(separatorArg);
        return separator is "," or ',';
    }

    private static SQLiteExpression QuantifierExpression(SQLVisitor visitor, MethodCallExpression node, string aggregateFunction, SQLiteExpression? filterExpression)
    {
        SQLiteExpression predicate;
        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            Expression resolvedExpression = visitor.Visit(lambda.Body);
            if (resolvedExpression is not SQLiteExpression sql)
            {
                throw new NotSupportedException($"{node.Method.Name} could not resolve the predicate.");
            }
            predicate = sql;
        }
        else
        {
            predicate = SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(), "1");
        }

        if (filterExpression == null)
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                $"{aggregateFunction}(CASE WHEN ", predicate, " THEN 1 ELSE 0 END)", predicate.Parameters);
        }

        string emptyValue = aggregateFunction == "MIN" ? "1" : "0";
        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
            $"COALESCE({aggregateFunction}(CASE WHEN ", predicate, " THEN 1 ELSE 0 END) FILTER (WHERE ", filterExpression, $"), {emptyValue})",
            ParameterHelpers.CombineParameters(predicate, filterExpression));
    }

    private static string ContainsColumnName(SQLTranslator translator)
    {
        if (translator.Selects.Count > 0)
        {
            return translator.Selects[0].IdentifierText;
        }

        string columnSql = translator.Visitor.TableColumns.Values.First().ToString()!;
        int end = columnSql.LastIndexOf('"');
        int start = columnSql.LastIndexOf('"', end - 1) + 1;
        return columnSql[start..end];
    }

    private static SQLiteExpression BuildCountExpression(SQLVisitor visitor, MethodCallExpression node, SQLiteExpression? filterExpression)
    {
        if (filterExpression == null)
        {
            return SQLiteExpression.Leaf(
                node.Method.ReturnType,
                visitor.Counters.NextIdentifier(),
                "COUNT(*)",
                []);
        }

        return SQLiteExpression.Wrap(
            node.Method.ReturnType,
            visitor.Counters.NextIdentifier(),
            "COUNT(*) FILTER (WHERE ",
            filterExpression,
            ")",
            filterExpression.Parameters);
    }

    private static Expression BuildInlineArrayContains(SQLVisitor visitor, MethodCallExpression node, NewArrayExpression source)
    {
        Expression item = node.Arguments[1];
        Expression predicate = Expression.Equal(source.Expressions[0], item);
        for (int i = 1; i < source.Expressions.Count; i++)
        {
            predicate = Expression.OrElse(predicate, Expression.Equal(source.Expressions[i], item));
        }

        return visitor.Visit(predicate);
    }

    private static SQLiteExpression AggregateExpression(SQLVisitor visitor, MethodCallExpression node, string aggregateFunction, SQLiteExpression? sqlExpression, SQLiteExpression? filterExpression)
    {
        SQLiteExpression target;
        if (node.Arguments.Count == 1)
        {
            target = sqlExpression!;
        }
        else
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            Expression resolvedExpression = visitor.Visit(lambda.Body);
            if (resolvedExpression is not SQLiteExpression sql)
            {
                throw new NotSupportedException("Sum could not resolve the expression.");
            }
            target = visitor.CoalesceLiftedOrderComparison(lambda.Body, sql);
            if (aggregateFunction is "MIN" or "MAX")
            {
                target = visitor.CastTextDecimalForOrdering(target);
            }
        }

        bool coalesce = aggregateFunction == "SUM";

        if (aggregateFunction is "MAX" or "MIN" && TypeHelpers.UnsignedIntegerKey(target.Type) == typeof(ulong))
        {
            string nonMatchSide = aggregateFunction == "MAX" ? "< 0" : ">= 0";
            if (filterExpression == null)
            {
                return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                    [$"COALESCE({aggregateFunction}(CASE WHEN ", $" {nonMatchSide} THEN ", $" END), {aggregateFunction}(", "))"],
                    [target, target, target],
                    target.Parameters);
            }

            return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                [$"COALESCE({aggregateFunction}(CASE WHEN ", $" {nonMatchSide} THEN ", " END) FILTER (WHERE ", $"), {aggregateFunction}(", ") FILTER (WHERE ", "))"],
                [target, target, filterExpression, target, filterExpression],
                ParameterHelpers.CombineParameters(target, filterExpression));
        }

        if (filterExpression == null)
        {
            SQLiteExpression aggregate = coalesce
                ? SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"COALESCE({aggregateFunction}(", target, "), 0)", target.Parameters)
                : SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{aggregateFunction}(", target, ")", target.Parameters);
            if (aggregateFunction is "MAX" or "MIN" && target.IsDayOfWeekInteger)
            {
                aggregate.WithDayOfWeekInteger();
            }

            return aggregate;
        }

        SQLiteExpression filtered = SQLiteExpression.Binary(
            node.Method.ReturnType,
            visitor.Counters.NextIdentifier(),
            coalesce ? $"COALESCE({aggregateFunction}(" : $"{aggregateFunction}(",
            target,
            ") FILTER (WHERE ",
            filterExpression,
            coalesce ? "), 0)" : ")",
            ParameterHelpers.CombineParameters(target, filterExpression));
        if (aggregateFunction is "MAX" or "MIN" && target.IsDayOfWeekInteger)
        {
            filtered.WithDayOfWeekInteger();
        }

        return filtered;
    }

    private static SQLiteExpression? BuildLocalCollectionExpression(SQLVisitor visitor, Type returnType, ParameterExpression element, Expression predicate, List<object?> items, bool isAny)
    {
        if (items.Count == 0)
        {
            return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), "0");
        }

        LocalCollectionPathCollector collector = new(element);
        collector.Visit(predicate);
        if (collector.ReadsWholeParameter && !TypeHelpers.IsSimple(element.Type, visitor.Database.Options))
        {
            return null;
        }

        List<KeyValuePair<string, Expression>> paths = collector.Paths.ToList();
        if (collector.ReadsWholeParameter || collector.HasNullCheck)
        {
            paths.Insert(0, new KeyValuePair<string, Expression>(string.Empty, element));
        }

        if (paths.Count == 0)
        {
            if (visitor.Visit(predicate) is not SQLiteExpression sqlPredicate)
            {
                return null;
            }

            return isAny
                ? sqlPredicate
                : SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), $"(CASE WHEN ", sqlPredicate, $" THEN {items.Count} ELSE 0 END)", sqlPredicate.Parameters);
        }

        HashSet<string> jsonComparedPaths = [];
        CollectJsonComparedPaths(visitor, predicate, element, jsonComparedPaths);

        string alias = $"l{visitor.Counters.NextTableIndex('l')}";
        Dictionary<string, Expression> localColumns = new(StringComparer.Ordinal);
        for (int i = 0; i < paths.Count; i++)
        {
            localColumns[paths[i].Key] = SQLiteExpression.Leaf(
                paths[i].Value.Type,
                visitor.Counters.NextIdentifier(),
                $"{alias}.\"column{i + 1}\"");
        }

        visitor.MethodArguments[element] = localColumns;
        visitor.OptionalRowColumns.Add(localColumns);
        SQLiteExpression sqlFilter;
        try
        {
            if (visitor.Visit(predicate) is not SQLiteExpression resolvedFilter)
            {
                return null;
            }

            sqlFilter = resolvedFilter;
        }
        finally
        {
            visitor.MethodArguments.Remove(element);
            visitor.OptionalRowColumns.Remove(localColumns);
        }

        List<SQLiteParameter> valueParameters = new(items.Count * paths.Count);
        StringBuilder valuesSql = StringBuilderPool.Rent();
        valuesSql.Append(isAny ? "EXISTS (SELECT 1 FROM (VALUES " : "(SELECT COUNT(*) FROM (VALUES ");
        for (int rowIndex = 0; rowIndex < items.Count; rowIndex++)
        {
            if (rowIndex > 0)
            {
                valuesSql.Append(", ");
            }

            valuesSql.Append('(');
            Expression replacement = Expression.Constant(items[rowIndex], element.Type);
            ParameterSubstitutor substitutor = new(element, replacement);
            for (int columnIndex = 0; columnIndex < paths.Count; columnIndex++)
            {
                if (columnIndex > 0)
                {
                    valuesSql.Append(", ");
                }

                Expression valueExpression = substitutor.Visit(paths[columnIndex].Value);
                bool complexElement = !TypeHelpers.IsSimple(element.Type, visitor.Database.Options);
                object? value = complexElement && items[rowIndex] == null
                    ? null
                    : paths[columnIndex].Key.Length == 0 && complexElement
                        ? 1
                        : ExpressionHelpers.GetConstantValue(valueExpression);
                Type declaredType = paths[columnIndex].Value.Type;
                if (paths[columnIndex].Key.Length == 0 && complexElement)
                {
                    declaredType = typeof(int);
                }
                if (jsonComparedPaths.Contains(paths[columnIndex].Key))
                {
                    value = JsonValueText.NormalizeInValue(visitor.Database.Options, isJsonSource: true, value);
                }

                string parameterName = visitor.Counters.NextParamName();
                valuesSql.Append(parameterName);
                valueParameters.Add(new SQLiteParameter
                {
                    Name = parameterName,
                    Value = value,
                    DeclaredType = declaredType,
                    InlineIfParameterLimitExceeded = true
                });
            }

            valuesSql.Append(')');
        }

        valuesSql.Append(") AS ");
        valuesSql.Append(alias);
        valuesSql.Append(" WHERE ");
        sqlFilter.WriteSqlTo(valuesSql);
        valuesSql.Append(')');

        SQLiteParameter[] parameters = [.. valueParameters, .. sqlFilter.Parameters ?? []];
        return SQLiteExpression.Leaf(
            returnType,
            visitor.Counters.NextIdentifier(),
            StringBuilderPool.ToStringAndReturn(valuesSql),
            parameters);
    }

    private static bool IsSafeLocalCollectionValue(Expression expression)
    {
        if (ExpressionHelpers.IsConstant(expression))
        {
            return true;
        }

        if (expression is not MethodCallExpression methodCall)
        {
            return false;
        }

        if (!IsSystemMethod(methodCall.Method))
        {
            return false;
        }

        return ExpressionHelpers.IsConstantMethodCall(methodCall);
    }

    private static Expression StripSpanConversion(Expression expression)
    {
        while (expression is MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } conversion)
        {
            expression = conversion.Arguments[0];
        }

        return expression;
    }

    private static void CollectJsonComparedPaths(SQLVisitor visitor, Expression expression, ParameterExpression element, HashSet<string> paths)
    {
        if (expression is not BinaryExpression binary)
        {
            return;
        }

        if (LocalCollectionPath(binary.Left, element) is { } leftPath
            && IsJsonSourceExpression(visitor, binary.Right, element))
        {
            paths.Add(leftPath);
        }

        if (LocalCollectionPath(binary.Right, element) is { } rightPath
            && IsJsonSourceExpression(visitor, binary.Left, element))
        {
            paths.Add(rightPath);
        }

        CollectJsonComparedPaths(visitor, binary.Left, element, paths);
        CollectJsonComparedPaths(visitor, binary.Right, element, paths);
    }

    private static bool IsJsonSourceExpression(SQLVisitor visitor, Expression expression, ParameterExpression element)
    {
        ParameterUsageFinderVisitor usage = new(element);
        usage.Visit(expression);
        if (usage.Found || ExpressionHelpers.IsConstant(expression))
        {
            return false;
        }

        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        if (expression is MemberExpression member
            && visitor.Database.Options.HasJsonConverter(member.Expression!.Type))
        {
            return true;
        }

        return visitor.ResolveExpression(expression).SQLiteExpression is { IsJsonSource: true };
    }

    private static string? LocalCollectionPath(Expression expression, ParameterExpression element)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            expression = convert.Operand;
        }

        if (expression == element)
        {
            return string.Empty;
        }

        if (expression is MemberExpression member)
        {
            (string path, ParameterExpression? root) = ExpressionHelpers.ResolveNullableParameterPath(member);
            return root == element ? path : null;
        }

        return null;
    }

    private static bool ConvertsToDatabaseNull(SQLVisitor visitor, object value, Type? declaredType)
    {
        return visitor.Database.Options.TryResolveWriteConverter(declaredType, value, out ISQLiteTypeConverter? converter)
            && converter.ToDatabase(value) is null;
    }

    private static object? NormalizeInListValue(SQLVisitor visitor, bool isJsonSource, object? value)
    {
        value = JsonValueText.NormalizeInValue(visitor.Database.Options, isJsonSource, value);
        if (visitor.Database.Options.DecimalStorage == DecimalStorageMode.Text && value is decimal decimalValue)
        {
            return (double)decimalValue;
        }

        return value;
    }

    private static SQLiteExpression BuildEnumerableContains(SQLVisitor visitor, MethodCallExpression node, IEnumerable enumerable, int itemIndex, ResolvedModel item)
    {
        SQLiteExpression itemExpression = visitor.PrepareKeyOperand(
            node.Arguments[itemIndex],
            item.SQLiteExpression!);
        Type itemType = node.Arguments[itemIndex].Type;
        List<object?> values = enumerable.Cast<object?>().ToList();
        if (DayOfWeekHelpers.IsComputedDayOfWeek(node.Arguments[itemIndex]) || itemExpression.IsDayOfWeekInteger)
        {
            itemType = typeof(int);
            values = values.Select(value => value is DayOfWeek dayOfWeek ? (object?)(int)dayOfWeek : value).ToList();
        }

        return BuildScalarInExpression(visitor, node.Method.ReturnType, itemExpression, itemType, values);
    }

    private static SQLiteExpression BuildScalarInExpression(SQLVisitor visitor, Type returnType, SQLiteExpression itemExpr, Type itemType, IReadOnlyList<object?> values)
    {
        itemExpr = visitor.CastTextDecimalForOrdering(itemExpr);
        bool hasNull = false;
        List<SQLiteParameter> valueParameters = new(values.Count);
        List<string> valueSql = new(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            object? value = values[i];
            if (value is null || ConvertsToDatabaseNull(visitor, value, itemType))
            {
                hasNull = true;
                continue;
            }

            value = NormalizeInListValue(visitor, itemExpr.IsJsonSource, value);

            SQLiteParameter parameter = new()
            {
                Name = visitor.Counters.NextParamName(),
                Value = value,
                DeclaredType = itemType,
                InlineIfParameterLimitExceeded = true
            };
            valueParameters.Add(parameter);
            valueSql.Add(parameter.Name);
        }

        SQLiteParameter[] parameters = valueParameters.ToArray();

        if (valueSql.Count == 0 && !hasNull)
        {
            return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), "0 = 1", itemExpr.Parameters);
        }

        if (valueSql.Count == 0)
        {
            return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "", itemExpr, " IS NULL", itemExpr.Parameters);
        }

        StringBuilder paramSb = new(" IN (");
        for (int i = 0; i < valueSql.Count; i++)
        {
            if (i > 0) paramSb.Append(", ");
            paramSb.Append(valueSql[i]);
        }

        paramSb.Append(')');

        SQLiteParameter[] allParameters = [.. itemExpr.Parameters ?? [], .. parameters];
        bool itemMayBeNull = !itemType.IsValueType || Nullable.GetUnderlyingType(itemType) != null;

        if (!hasNull)
        {
            if (itemMayBeNull)
            {
                return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "((", itemExpr, paramSb.ToString() + ") IS 1)", allParameters);
            }

            return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "", itemExpr, paramSb.ToString(), allParameters);
        }

        return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
            ["(", paramSb.ToString() + " OR ", " IS NULL)"],
            [itemExpr, itemExpr],
            allParameters);
    }
}
