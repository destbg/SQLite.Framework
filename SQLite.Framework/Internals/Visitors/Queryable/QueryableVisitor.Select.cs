namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array element type comes from the user projection.")]
    private Expression VisitSelect(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);

        if (JoinSelectExpression != null && lambda.Body is ParameterExpression)
        {
            return JoinSelectExpression;
        }

        if (lambda.Body is not ParameterExpression)
        {
            ThrowIfSetOperations(node.Method.Name);
        }

        lambda = CommonHelpers.ExpandRowsInMethodCalls(lambda, visitor.MethodArguments.Keys);

        if (PreviousSelectLambda is { Body: MemberInitExpression prevMie }
            && lambda.Body is MemberExpression outerMa
            && outerMa.Expression is ParameterExpression outerParam
            && outerParam == lambda.Parameters[0]
            && !prevMie.Bindings.OfType<MemberAssignment>().Any(mb => mb.Member.Name == outerMa.Member.Name))
        {
            throw new NotSupportedException(
                $"Chained Select cannot read '{outerMa.Member.Name}': the inner projection did not initialize that member. " +
                $"Include '{outerMa.Member.Name}' in the inner projection or restructure the query.");
        }

        if (database.Options.SelectMaterializers.Count > 0)
        {
            if (lambda.Body is not ParameterExpression || PreviousSelectLambda != null)
            {
                Expression signatureBody = lambda.Body;
                if (PreviousSelectLambda != null)
                {
                    Expression? flattened = TryFlattenChainedSelectBody(lambda, PreviousSelectLambda);
                    if (flattened != null)
                    {
                        signatureBody = flattened;
                    }
                }

                ConstructedConditionalFinderVisitor shape = new();
                shape.Visit(signatureBody);
                RawSelectSignature = SelectSignature.Compute(signatureBody);
                LastSelectLambdaBody = signatureBody;

                if (!ReferenceEquals(signatureBody, lambda.Body)
                    && shape.Found
                    && PreviousSelectSourceColumns != null
                    && database.Options.SelectMaterializers.ContainsKey(RawSelectSignature))
                {
                    lambda = Expression.Lambda(signatureBody, PreviousSelectLambda!.Parameters);
                    visitor.TableColumns = PreviousSelectSourceColumns;
                }
            }
        }

        PreviousSelectLambda = lambda;
        PreviousSelectSourceColumns = visitor.TableColumns;
        visitor.IsInSelectProjection = true;
        visitor.ClientEvalAllowed = !IsInnerQuery;
        visitor.TableColumns = aliasVisitor.ResolveResultAlias(lambda);

        Selects.Clear();

        if (visitor.TableColumns.All(f => f.Value is SQLiteExpression)
            && lambda.Body is not NewArrayExpression
            && !IsScalarBoxingToObject(lambda.Body)
            && !HasRowReferencingListBinding(lambda))
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                SQLiteExpression sqlExpression = (SQLiteExpression)tableColumn.Value;
                if (visitor.TableColumns.Count == 1)
                {
                    sqlExpression = visitor.CoalesceLiftedOrderComparison(lambda.Body, sqlExpression);
                }

                SQLiteExpression newSqlExpression = SQLiteExpression.Alias(
                    node.Method.ReturnType,
                    visitor.Counters.NextIdentifier(),
                    sqlExpression,
                    sqlExpression.Parameters
                );

                if (sqlExpression.IsDayOfWeekInteger)
                {
                    newSqlExpression.WithDayOfWeekInteger();
                }

                if (!string.IsNullOrEmpty(tableColumn.Key))
                {
                    newSqlExpression.IdentifierText = tableColumn.Key;
                    SelectValueTypes[tableColumn.Key] = sqlExpression.Type;
                }

                Selects.Add(newSqlExpression);
            }

            if (lambda.Body is MemberInitExpression mieBody && mieBody.Bindings.OfType<MemberListBinding>().Any())
            {
                List<MemberBinding> allBindings = RebuildBindingsForListInit(mieBody, prefix: null);

                visitor.IsInSelectProjection = false;
                visitor.ClientEvalAllowed = false;
                return Expression.MemberInit(mieBody.NewExpression, allBindings);
            }

            visitor.IsInSelectProjection = false;
            visitor.ClientEvalAllowed = false;
            return node;
        }

        if (lambda.Body is ParameterExpression)
        {
            List<PropertyInfo> properties = visitor.TableColumns
                .Select(tableColumn => lambda.Body.Type.GetProperty(tableColumn.Key)!)
                .ToList();

            ConstructorInfo constructor = lambda.Body.Type.GetConstructors()[0];

            List<Expression> constructorArgs = properties
                .ConvertAll(prop => selectVisitor.Visit(
                    visitor.TableColumns.First(tc => tc.Key == prop.Name).Value));

            bool hasWritableProperties = properties.All(p => p.CanWrite);

            visitor.IsInSelectProjection = false;
            visitor.ClientEvalAllowed = false;

            if (hasWritableProperties)
            {
                List<MemberAssignment> memberBindings = properties
                    .ConvertAll(prop => Expression.Bind(prop, constructorArgs[properties.IndexOf(prop)]));

                return Expression.MemberInit(
                    Expression.New(constructor, constructorArgs, properties),
                    memberBindings
                );
            }
            else
            {
                return Expression.New(constructor, constructorArgs, properties);
            }
        }

        visitor.ClientEvalUsed = false;
        Expression selectBody = NormalizeMemberInitOrder(lambda.Body);
        Expression normalSelect;
        if (selectBody is NewArrayExpression arrayBody)
        {
            Type elementType = arrayBody.Type.GetElementType()!;
            List<Expression> elements = arrayBody.Expressions
                .Select(e => CoerceArrayElement(visitor.Visit(e)!, elementType))
                .ToList();
            normalSelect = Expression.NewArrayInit(elementType, elements);
        }
        else
        {
            normalSelect = visitor.Visit(selectBody);
            if (normalSelect is SQLiteExpression projectionExpression)
            {
                normalSelect = visitor.CoalesceLiftedOrderComparison(selectBody, projectionExpression);
            }
        }
        Expression selectExpression = visitor.ClientEvalUsed
            ? visitor.ToClientExpression(lambda.Body)
            : normalSelect;
        visitor.IsInSelectProjection = false;
        visitor.ClientEvalAllowed = false;
        Expression expression = selectVisitor.Visit(selectExpression);

        return expression;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Builds an expression tree for the translator.")]
    private MethodCallExpression VisitSelectMany(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        LambdaExpression selector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        LambdaExpression resultSelector;
        if (node.Arguments.Count >= 3)
        {
            resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);
        }
        else
        {
            Type innerElementType = TypeHelpers.GetEnumerableElementType(selector.Body.Type)!;
            ParameterExpression outerParam = Expression.Parameter(selector.Parameters[0].Type, "s");
            ParameterExpression innerParam = Expression.Parameter(innerElementType, "x");
            resultSelector = Expression.Lambda(innerParam, outerParam, innerParam);
        }

        Expression flattenSource = selector.Body;
        bool hasDefaultIfEmpty = false;

        if (flattenSource is MethodCallExpression { Method.Name: nameof(Enumerable.DefaultIfEmpty) } methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count > 1)
            {
                throw new NotSupportedException(
                    "DefaultIfEmpty with a custom default value is not supported in a query.");
            }

            hasDefaultIfEmpty = true;
            flattenSource = methodCallExpression.Arguments[0];
        }

        Expression chainRoot = flattenSource;
        while (chainRoot is MethodCallExpression { Object: null, Arguments.Count: > 0 } chainCall
            && chainCall.Method.DeclaringType == typeof(Enumerable))
        {
            chainRoot = chainCall.Arguments[0];
        }

        JoinInfo? groupJoin = chainRoot is MemberExpression && chainRoot.Type.IsGenericType
            ? Joins.FirstOrDefault(f => f.EntityType == chainRoot.Type.GetGenericArguments()[^1] && f.IsGroupJoin)
            : null;

        if (hasDefaultIfEmpty || groupJoin != null)
        {
            List<LambdaExpression> groupFilters = [];

            while (flattenSource is MethodCallExpression { Method.Name: nameof(Enumerable.Where) } filterCall
                && filterCall.Method.DeclaringType == typeof(Enumerable)
                && ExpressionHelpers.StripQuotes(filterCall.Arguments[1]) is LambdaExpression { Parameters.Count: 1 } filterLambda)
            {
                groupFilters.Add(filterLambda);
                flattenSource = filterCall.Arguments[0];
            }

            groupFilters.Reverse();

            if (groupJoin == null || flattenSource is not MemberExpression memberExpression)
            {
                throw new NotSupportedException(
                    "DefaultIfEmpty and group filters are only supported directly on a group join group, as in " +
                    "'join x in source on ... into g from x in g.Where(x => x.Active).DefaultIfEmpty()'. " +
                    "A bare DefaultIfEmpty on a second from source is not supported and only plain " +
                    "'Where(x => ...)' filters may sit between the group and DefaultIfEmpty.");
            }

            groupJoin.JoinType = hasDefaultIfEmpty ? "LEFT JOIN" : "JOIN";
            groupJoin.IsGroupJoin = false;

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;

            Dictionary<string, Expression> result = [];

            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                if (tableColumn.Key.StartsWith(memberExpression.Member.Name + '.'))
                {
                    string path = tableColumn.Key[(memberExpression.Member.Name.Length + 1)..];
                    result.Add(path, tableColumn.Value);
                }
            }

            visitor.MethodArguments[resultSelector.Parameters[1]] = result;

            foreach (LambdaExpression groupFilter in groupFilters)
            {
                visitor.MethodArguments[groupFilter.Parameters[0]] = result;
                Expression filterResult = visitor.Visit(groupFilter.Body);

                if (filterResult is not SQLiteExpression filterSql)
                {
                    throw new NotSupportedException($"Unsupported WHERE expression {groupFilter.Body}");
                }

                groupJoin.OnClause = AndOnClause(groupJoin.OnClause!, filterSql);
            }

            resultSelector = CommonHelpers.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);
        }
        else
        {
            (Dictionary<string, Expression> newTableColumns, Type entityType, SQLiteExpression sql) = ResolveTable(selector.Body);

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

            resultSelector = CommonHelpers.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = "CROSS JOIN",
                Sql = sql,
                OnClause = null,
                IsGroupJoin = false
            });
        }

        bool isProjection = resultSelector.Body is NewExpression or MemberInitExpression;

        if (isProjection)
        {
            if (database.Options.SelectMaterializers.Count > 0)
            {
                RawSelectSignature = SelectSignature.Compute(resultSelector.Body);
                LastSelectLambdaBody = resultSelector.Body;
            }
        }

        if (isProjection && visitor.TableColumns.Values.Any(v => v is not SQLiteExpression))
        {
            visitor.IsInSelectProjection = true;
            visitor.ClientEvalAllowed = !IsInnerQuery;

            Expression decomposed = visitor.ToClientExpression(resultSelector.Body);
            if (decomposed is NewExpression { Members: not null } newExpression)
            {
                visitor.TableColumns = DecomposeJoinProjectionColumns(newExpression);
            }
            else
            {
                Selects.Clear();
                JoinSelectExpression = selectVisitor.Visit(decomposed);
            }

            visitor.IsInSelectProjection = false;
            visitor.ClientEvalAllowed = false;
        }

        return node;
    }

    private List<MemberBinding> RebuildBindingsForListInit(MemberInitExpression mie, string? prefix)
    {
        List<MemberBinding> rebuilt = [];

        foreach (MemberBinding binding in mie.Bindings)
        {
            if (binding is MemberAssignment ma)
            {
                string key = prefix is null ? ma.Member.Name : $"{prefix}.{ma.Member.Name}";

                if (ma.Expression is MemberInitExpression nestedMie)
                {
                    List<MemberBinding> nested = RebuildBindingsForListInit(nestedMie, key);
                    rebuilt.Add(Expression.Bind(ma.Member, Expression.MemberInit(nestedMie.NewExpression, nested)));
                }
                else if (visitor.TableColumns.TryGetValue(key, out Expression? colExpr) && colExpr is SQLiteExpression sqlExpr)
                {
                    Type memberType = ma.Member is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)ma.Member).FieldType;
                    SQLiteExpression compilerExpr = SQLiteExpression.Alias(memberType, 0, sqlExpr, null);
                    compilerExpr.IdentifierText = key;
                    rebuilt.Add(Expression.Bind(ma.Member, compilerExpr));
                }
            }
            else if (binding is MemberListBinding lb)
            {
                rebuilt.Add(lb);
            }
        }

        return rebuilt;
    }

    private Expression CoerceArrayElement(Expression element, Type elementType)
    {
        if (element is SQLiteExpression sql && sql.Type != elementType)
        {
            return SQLiteExpression.Alias(elementType, visitor.Counters.NextIdentifier(), sql, sql.Parameters);
        }

        return element;
    }

    private static SQLiteExpression AndOnClause(SQLiteExpression onClause, SQLiteExpression filter)
    {
        SQLiteExpression left = ExpressionHelpers.BracketIfNeeded(onClause);
        SQLiteExpression right = ExpressionHelpers.BracketIfNeeded(filter);
        return SQLiteExpression.Binary(typeof(bool), -1, "", left, " AND ", right, "", ParameterHelpers.CombineParameters(left, right));
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Projection types are part of the client assembly.")]
    private static Expression NormalizeMemberInitOrder(Expression body)
    {
        if (body is not MemberInitExpression memberInit)
        {
            return body;
        }

        PropertyInfo[] declaredProperties = memberInit.Type.GetProperties();
        List<MemberBinding> ordered = memberInit.Bindings
            .OrderBy(binding => Array.FindIndex(declaredProperties, p => p.Name == binding.Member.Name))
            .ToList();
        return memberInit.Update(memberInit.NewExpression, ordered);
    }

    private static bool HasRowReferencingListBinding(LambdaExpression lambda)
    {
        if (lambda.Body is not MemberInitExpression memberInit)
        {
            return false;
        }

        foreach (MemberListBinding listBinding in memberInit.Bindings.OfType<MemberListBinding>())
        {
            foreach (Expression element in listBinding.Initializers.SelectMany(initializer => initializer.Arguments))
            {
                ParameterUsageFinderVisitor finder = new(lambda.Parameters[0]);
                finder.Visit(element);
                if (finder.Found)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsScalarBoxingToObject(Expression body)
    {
        return body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            && convert.Type == typeof(object)
            && convert.Operand.Type != typeof(object);
    }

    private static Expression? TryFlattenChainedSelectBody(LambdaExpression outer, LambdaExpression inner)
    {
        if (outer.Parameters[0].Type != inner.Body.Type)
        {
            return null;
        }

        if (outer.Body is MemberExpression outerMa
            && outerMa.Expression is ParameterExpression outerParam1
            && outerParam1 == outer.Parameters[0]
            && inner.Body is MemberInitExpression innerMie)
        {
            MemberAssignment match = innerMie.Bindings.OfType<MemberAssignment>()
                .First(b => b.Member.Name == outerMa.Member.Name);
            return match.Expression;
        }

        ParameterExpression outerParam = outer.Parameters[0];
        ParameterSubstitutor substitutor = new(outerParam, inner.Body);
        Expression rewritten = substitutor.Visit(outer.Body);
        if (rewritten != outer.Body)
        {
            return rewritten;
        }

        return null;
    }
}
