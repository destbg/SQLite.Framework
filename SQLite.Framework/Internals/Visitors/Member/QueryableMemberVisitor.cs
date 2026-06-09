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
            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"({Environment.NewLine}{querySql}{Environment.NewLine})", queryParams);
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
        return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
            $"EXISTS ({Environment.NewLine}SELECT 1 FROM ({Environment.NewLine}{querySql}{Environment.NewLine}) WHERE \"{containsColumn}\" IS ", firstArg, ")", parameters);
    }

    public static Expression HandleEnumerableMethod(SQLVisitor visitor, MethodCallExpression node, IEnumerable enumerable, List<ResolvedModel> arguments)
    {
        int firstItemArgIndex = node.Object == null ? 1 : 0;

        if (arguments.Skip(firstItemArgIndex).Any(f => f.SQLiteExpression == null))
        {
            return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Object == null
            && TypeHelpers.IsSimple(node.Method.ReturnType, visitor.Database.Options)
            && arguments.Skip(firstItemArgIndex).All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, [
                enumerable,
                ..node.Arguments.Skip(1).Select(ExpressionHelpers.GetConstantValue)
            ]);
            string pName = visitor.Counters.NextParamName();

            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
            {
                List<object?> values = enumerable.Cast<object?>().ToList();
                bool hasNull = values.Any(v => v is null);
                SQLiteParameter[] parameters = values
                    .Where(v => v is not null)
                    .Select(f => new SQLiteParameter
                    {
                        Name = visitor.Counters.NextParamName(),
                        Value = f
                    })
                    .ToArray();

                int itemIndex = node.Object == null ? 1 : 0;
                ResolvedModel item = arguments[itemIndex];
                SQLiteExpression itemExpr = item.SQLiteExpression!;

                if (parameters.Length == 0 && !hasNull)
                {
                    // For an empty list, `IN ()` is invalid SQL and should always return false.
                    // We use `0 = 1` to ensure the condition is never true.
                    return SQLiteExpression.Leaf(
                        node.Method.ReturnType,
                        visitor.Counters.NextIdentifier(),
                        "0 = 1",
                        item.Parameters
                    );
                }

                if (parameters.Length == 0)
                {
                    return SQLiteExpression.Wrap(
                        node.Method.ReturnType,
                        visitor.Counters.NextIdentifier(),
                        "", itemExpr, " IS NULL",
                        item.Parameters
                    );
                }

                StringBuilder paramSb = new(" IN (");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) paramSb.Append(", ");
                    paramSb.Append(parameters[i].Name);
                }
                paramSb.Append(')');

                SQLiteParameter[] allParameters = [.. item.Parameters ?? [], .. parameters];

                Type itemType = node.Arguments[itemIndex].Type;
                bool itemMayBeNull = !itemType.IsValueType || Nullable.GetUnderlyingType(itemType) != null;

                if (!hasNull)
                {
                    if (itemMayBeNull)
                    {
                        return SQLiteExpression.Wrap(
                            node.Method.ReturnType,
                            visitor.Counters.NextIdentifier(),
                            "((", itemExpr, paramSb.ToString() + ") IS 1)",
                            allParameters
                        );
                    }

                    return SQLiteExpression.Wrap(
                        node.Method.ReturnType,
                        visitor.Counters.NextIdentifier(),
                        "", itemExpr, paramSb.ToString(),
                        allParameters
                    );
                }

                return SQLiteExpression.Multi(
                    node.Method.ReturnType,
                    visitor.Counters.NextIdentifier(),
                    ["(", paramSb.ToString() + " OR ", " IS NULL)"],
                    [itemExpr, itemExpr],
                    allParameters
                );
            }
        }

        return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
    }

    public static Expression HandleGroupingMethod(SQLVisitor visitor, MethodCallExpression node)
    {
        Expression receiver = node.Arguments[0];
        LambdaExpression? filterLambda = TryPeelWhereFilter(ref receiver);

        bool countWithPredicate = filterLambda == null
            && node.Method.Name is nameof(Enumerable.Count) or nameof(Enumerable.LongCount)
            && node.Arguments.Count == 2;
        if (countWithPredicate)
        {
            filterLambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        }

        Dictionary<string, Expression>? newTableColumns = null;
        SQLiteExpression? sqlExpression = null;

        if (node.Arguments.Count == 2 || filterLambda != null)
        {
            newTableColumns = BuildGroupingColumnMap(visitor, receiver);
        }

        SQLiteExpression? filterExpression = null;
        if (filterLambda != null)
        {
            visitor.MethodArguments[filterLambda.Parameters[0]] = newTableColumns!;
            Expression resolvedFilter = visitor.Visit(filterLambda.Body);
            if (resolvedFilter is not SQLiteExpression sqlFilter)
            {
                throw new NotSupportedException("Aggregate FILTER predicate could not be resolved.");
            }
            filterExpression = sqlFilter;
#if SQLITE_FRAMEWORK_VERSION_AWARE
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "FILTER (WHERE ...) on aggregates");
#endif
        }

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
            _ => throw new NotSupportedException($"Grouping aggregate {node.Method.Name} is not translatable to SQL.")
        };
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

        foreach (KeyValuePair<string, Expression> kvp in visitor.MethodArguments[pe])
        {
            if (kvp.Key.StartsWith(Constants.GroupingElementPrefix, StringComparison.Ordinal))
            {
                newTableColumns[kvp.Key[Constants.GroupingElementPrefix.Length..]] = kvp.Value;
                continue;
            }

            if (kvp.Key.StartsWith(path))
            {
                // +1 for the dot between the path and the key
                int length = path.Length + nameof(IGrouping<,>.Key).Length + 1;
                string[] split = kvp.Key[Math.Min(length, kvp.Key.Length)..]
                    .Split('.', StringSplitOptions.RemoveEmptyEntries);

                string newKey = string.Join('.', split);
                newTableColumns[newKey] = kvp.Value;
            }
        }

        return newTableColumns;
    }

    public static bool CheckConstantMethod<T>(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, [MaybeNullWhen(false)] out Expression expression)
    {
        if (arguments.Any(f => f.SQLiteExpression == null))
        {
            expression = Expression.Call(node.Method, arguments.Select(f => f.Expression));
            return true;
        }

        Type type = typeof(T);

        if (node.Method.ReturnType.IsAssignableTo(type) && arguments.All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, arguments.Select(f => f.Constant).ToArray());

            string pName = visitor.Counters.NextParamName();
            expression = SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
            return true;
        }

        expression = null;
        return false;
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
            target = sql;
        }

        bool coalesce = aggregateFunction == "SUM";

        Type targetType = Nullable.GetUnderlyingType(target.Type) ?? target.Type;
        if (aggregateFunction is "MAX" or "MIN" && targetType == typeof(ulong) && filterExpression == null)
        {
            string nonMatchSide = aggregateFunction == "MAX" ? "< 0" : ">= 0";
            return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                [$"COALESCE({aggregateFunction}(CASE WHEN ", $" {nonMatchSide} THEN ", $" END), {aggregateFunction}(", "))"],
                [target, target, target],
                target.Parameters);
        }

        if (filterExpression == null)
        {
            return coalesce
                ? SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"COALESCE({aggregateFunction}(", target, "), 0)", target.Parameters)
                : SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{aggregateFunction}(", target, ")", target.Parameters);
        }

        return SQLiteExpression.Binary(
            node.Method.ReturnType,
            visitor.Counters.NextIdentifier(),
            coalesce ? $"COALESCE({aggregateFunction}(" : $"{aggregateFunction}(",
            target,
            ") FILTER (WHERE ",
            filterExpression,
            coalesce ? "), 0)" : ")",
            ParameterHelpers.CombineParameters(target, filterExpression));
    }
}
