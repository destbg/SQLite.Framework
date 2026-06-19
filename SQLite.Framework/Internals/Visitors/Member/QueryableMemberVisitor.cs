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
        ComparerArgumentGuard.ThrowIfComparer(node);

        int firstItemArgIndex = node.Object == null ? 1 : 0;

        if (arguments.Skip(firstItemArgIndex).Any(f => f.SQLiteExpression == null))
        {
            return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Object == null
            && TypeHelpers.IsSimple(node.Method.ReturnType, visitor.Database.Options)
            && arguments.Skip(firstItemArgIndex).All(f => f.IsConstant))
        {
            ParameterInfo[] methodParameters = node.Method.GetParameters();
            object? result;
            if (methodParameters.Length > 0 && methodParameters[0].ParameterType.IsByRefLike)
            {
                result = node.Method.Name switch
                {
                    nameof(Enumerable.Contains) => enumerable.Cast<object?>().Contains(ExpressionHelpers.GetConstantValue(node.Arguments[1])),
                    _ => throw new NotSupportedException(
                        $"{node.Method.Name} over a constant collection is not translatable to SQL. " +
                        "Materialize the collection into a List<T> first.")
                };
            }
            else
            {
                result = node.Method.Invoke(null, [
                    enumerable,
                    ..node.Arguments.Skip(1).Select(ExpressionHelpers.GetConstantValue)
                ]);
            }

            string pName = visitor.Counters.NextParamName();

            return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
            {
                int itemIndex = node.Object == null ? 1 : 0;
                SQLiteExpression itemExpr = arguments[itemIndex].SQLiteExpression!;
                Type itemType = node.Arguments[itemIndex].Type;
                List<object?> values = enumerable.Cast<object?>().ToList();
                return BuildScalarInExpression(visitor, node.Method.ReturnType, itemExpr, itemType, values);
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
            nameof(Enumerable.Any) => QuantifierExpression(visitor, node, "MAX", filterExpression),
            nameof(Enumerable.All) => QuantifierExpression(visitor, node, "MIN", filterExpression),
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

            if (kvp.Key == nameof(IGrouping<,>.Key))
            {
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

        if (node.Object == null && node.Method.ReturnType.IsAssignableTo(type) && arguments.All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, arguments.Select(f => f.Constant).ToArray());

            string pName = visitor.Counters.NextParamName();
            expression = SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), pName, result);
            return true;
        }

        expression = null;
        return false;
    }

    public static Expression? TryHandleConstantAnyPredicate(SQLVisitor visitor, MethodCallExpression node)
    {
        if (node.Method.Name != nameof(Enumerable.Any)
            || node.Arguments.Count != 2)
        {
            return null;
        }

        if (!ExpressionHelpers.IsConstant(node.Arguments[0])
            || ExpressionHelpers.GetConstantValue(node.Arguments[0]) is not IEnumerable enumerable)
        {
            return null;
        }

        if (ExpressionHelpers.StripQuotes(node.Arguments[1]) is not LambdaExpression { Parameters.Count: 1 } lambda)
        {
            return null;
        }

        ParameterExpression element = lambda.Parameters[0];
        List<Expression> conjuncts = [];
        FlattenAndAlso(lambda.Body, conjuncts);

        List<Expression> keySides = new(conjuncts.Count);
        List<Expression> valueSides = new(conjuncts.Count);
        foreach (Expression conjunct in conjuncts)
        {
            if (conjunct is not BinaryExpression { NodeType: ExpressionType.Equal } equality)
            {
                return null;
            }

            ParameterReferenceVisitor leftRefs = new(element);
            leftRefs.Visit(equality.Left);
            ParameterReferenceVisitor rightRefs = new(element);
            rightRefs.Visit(equality.Right);

            if (leftRefs.ReferencesTarget == rightRefs.ReferencesTarget)
            {
                return null;
            }

            bool leftIsValue = leftRefs.ReferencesTarget;
            ParameterReferenceVisitor valueRefs = leftIsValue ? leftRefs : rightRefs;
            if (valueRefs.ReferencesOther)
            {
                return null;
            }

            valueSides.Add(StripConversions(leftIsValue ? equality.Left : equality.Right));
            keySides.Add(StripConversions(leftIsValue ? equality.Right : equality.Left));
        }

        List<object?[]>? rows = MaterializeRows(enumerable, valueSides, element);
        if (rows == null)
        {
            return null;
        }

        List<SQLiteExpression> keyColumns = new(keySides.Count);
        foreach (Expression keySide in keySides)
        {
            SQLiteExpression? keyColumn = visitor.TryResolveColumnLeaf(keySide);
            if (keyColumn == null)
            {
                return null;
            }

            keyColumns.Add(keyColumn);
        }

        Type returnType = node.Method.ReturnType;
        if (keyColumns.Count == 1)
        {
            List<object?> values = new(rows.Count);
            foreach (object?[] row in rows)
            {
                values.Add(row[0]);
            }

            return BuildScalarInExpression(visitor, returnType, keyColumns[0], valueSides[0].Type, values);
        }

        return BuildRowValueInExpression(visitor, returnType, keyColumns, rows);
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

        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
            $"{aggregateFunction}(CASE WHEN ", predicate, " THEN 1 ELSE 0 END) FILTER (WHERE ", filterExpression, ")",
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

    private static Expression StripConversions(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            expression = convert.Operand;
        }

        return expression;
    }

    private static void FlattenAndAlso(Expression expression, List<Expression> conjuncts)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
        {
            FlattenAndAlso(andAlso.Left, conjuncts);
            FlattenAndAlso(andAlso.Right, conjuncts);
            return;
        }

        conjuncts.Add(expression);
    }

    private static List<object?[]>? MaterializeRows(IEnumerable enumerable, List<Expression> valueSides, ParameterExpression element)
    {
        List<object?[]> rows = [];
        foreach (object? item in enumerable)
        {
            ParameterSubstitutor substitutor = new(element, Expression.Constant(item, element.Type));
            object?[] row = new object?[valueSides.Count];
            for (int i = 0; i < valueSides.Count; i++)
            {
                Expression resolved = substitutor.Visit(valueSides[i]);
                if (!ExpressionHelpers.IsConstant(resolved))
                {
                    return null;
                }

                row[i] = ExpressionHelpers.GetConstantValue(resolved);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static SQLiteExpression BuildScalarInExpression(SQLVisitor visitor, Type returnType, SQLiteExpression itemExpr, Type itemType, IReadOnlyList<object?> values)
    {
        bool hasNull = false;
        List<SQLiteParameter> valueParameters = new(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                hasNull = true;
                continue;
            }

            valueParameters.Add(new SQLiteParameter
            {
                Name = visitor.Counters.NextParamName(),
                Value = values[i]
            });
        }

        SQLiteParameter[] parameters = valueParameters.ToArray();

        if (parameters.Length == 0 && !hasNull)
        {
            return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), "0 = 1", itemExpr.Parameters);
        }

        if (parameters.Length == 0)
        {
            return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "", itemExpr, " IS NULL", itemExpr.Parameters);
        }

        StringBuilder paramSb = new(" IN (");
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0) paramSb.Append(", ");
            paramSb.Append(parameters[i].Name);
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

    private static SQLiteExpression BuildRowValueInExpression(SQLVisitor visitor, Type returnType, List<SQLiteExpression> keyColumns, List<object?[]> rows)
    {
        int columnCount = keyColumns.Count;

        List<object?[]> pureRows = [];
        List<object?[]> nullRows = [];
        foreach (object?[] row in rows)
        {
            bool rowHasNull = false;
            for (int c = 0; c < columnCount; c++)
            {
                if (row[c] is null)
                {
                    rowHasNull = true;
                    break;
                }
            }

            (rowHasNull ? nullRows : pureRows).Add(row);
        }

        if (pureRows.Count == 0 && nullRows.Count == 0)
        {
            return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), "0 = 1", ParameterHelpers.CombineParameters(keyColumns));
        }

        bool emitInClause = pureRows.Count > 0;
#if SQLITE_FRAMEWORK_VERSION_AWARE
        emitInClause = emitInClause && visitor.Database.Options.OverMinimumVersion(SQLiteMinimumVersion.V3_15);
#endif

        bool anyKeyNullable = false;
        foreach (SQLiteExpression keyColumn in keyColumns)
        {
            Type keyType = keyColumn.Type;
            if (!keyType.IsValueType || Nullable.GetUnderlyingType(keyType) != null)
            {
                anyKeyNullable = true;
                break;
            }
        }

        List<object?[]> orRows = emitInClause ? nullRows : rows;
        bool wrapIsOne = emitInClause && anyKeyNullable;
        int clauseCount = (emitInClause ? 1 : 0) + orRows.Count;

        List<string> parts = [];
        List<SQLiteExpression> children = [];
        List<SQLiteParameter> valueParameters = [];
        StringBuilder pending = StringBuilderPool.Rent();
        pending.Append(wrapIsOne ? "((" : clauseCount >= 2 ? "(" : "");

        bool firstClause = true;
        if (emitInClause)
        {
            pending.Append('(');
            for (int c = 0; c < columnCount; c++)
            {
                if (c > 0) pending.Append(", ");
                parts.Add(pending.ToString());
                pending.Clear();
                children.Add(keyColumns[c]);
            }

            pending.Append(") IN (");
            for (int r = 0; r < pureRows.Count; r++)
            {
                if (r > 0) pending.Append(", ");
                pending.Append('(');
                for (int c = 0; c < columnCount; c++)
                {
                    if (c > 0) pending.Append(", ");
                    string paramName = visitor.Counters.NextParamName();
                    valueParameters.Add(new SQLiteParameter
                    {
                        Name = paramName,
                        Value = pureRows[r][c]
                    });
                    pending.Append(paramName);
                }

                pending.Append(')');
            }

            pending.Append(')');
            firstClause = false;
        }

        foreach (object?[] row in orRows)
        {
            if (!firstClause) pending.Append(" OR ");
            pending.Append('(');
            for (int c = 0; c < columnCount; c++)
            {
                if (c > 0) pending.Append(" AND ");
                parts.Add(pending.ToString());
                pending.Clear();
                children.Add(keyColumns[c]);
                pending.Append(" IS ");
                if (row[c] is null)
                {
                    pending.Append("NULL");
                }
                else
                {
                    string paramName = visitor.Counters.NextParamName();
                    valueParameters.Add(new SQLiteParameter { Name = paramName, Value = row[c] });
                    pending.Append(paramName);
                }
            }

            pending.Append(')');
            firstClause = false;
        }

        pending.Append(wrapIsOne ? ") IS 1)" : clauseCount >= 2 ? ")" : "");
        parts.Add(StringBuilderPool.ToStringAndReturn(pending));

        return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(), parts.ToArray(), children.ToArray(), valueParameters.ToArray());
    }
}
