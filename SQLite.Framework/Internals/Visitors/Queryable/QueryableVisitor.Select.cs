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

        lambda = RowParameterExpander.ExpandRowsInMethodCalls(lambda, visitor.MethodArguments.Keys);

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

                RawSelectSignature = SelectSignature.Compute(signatureBody);
                LastSelectLambdaBody = signatureBody;
            }
        }

        PreviousSelectLambda = lambda;
        visitor.IsInSelectProjection = true;
        visitor.ClientEvalAllowed = !IsInnerQuery;
        visitor.TableColumns = aliasVisitor.ResolveResultAlias(lambda);

        Selects.Clear();

        if (visitor.TableColumns.All(f => f.Value is SQLiteExpression)
            && lambda.Body is not NewArrayExpression
            && !IsScalarBoxingToObject(lambda.Body))
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                SQLiteExpression sqlExpression = (SQLiteExpression)tableColumn.Value;

                SQLiteExpression newSqlExpression = SQLiteExpression.Alias(
                    node.Method.ReturnType,
                    visitor.Counters.NextIdentifier(),
                    sqlExpression,
                    sqlExpression.Parameters
                );

                if (!string.IsNullOrEmpty(tableColumn.Key))
                {
                    newSqlExpression.IdentifierText = tableColumn.Key;
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
        Expression normalSelect;
        if (lambda.Body is NewArrayExpression arrayBody)
        {
            Type elementType = arrayBody.Type.GetElementType()!;
            List<Expression> elements = arrayBody.Expressions
                .Select(e => CoerceArrayElement(visitor.Visit(e)!, elementType))
                .ToList();
            normalSelect = Expression.NewArrayInit(elementType, elements);
        }
        else
        {
            normalSelect = visitor.Visit(lambda.Body);
        }
        Expression selectExpression = visitor.ClientEvalUsed
            ? visitor.ToClientExpression(lambda.Body)
            : normalSelect;
        visitor.IsInSelectProjection = false;
        visitor.ClientEvalAllowed = false;
        Expression expression = selectVisitor.Visit(selectExpression);

        return expression;
    }

    private MethodCallExpression VisitSelectMany(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        LambdaExpression selector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        LambdaExpression resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);

        if (selector.Body is MethodCallExpression { Method.Name: nameof(Enumerable.DefaultIfEmpty) } methodCallExpression)
        {
            Type type = selector.Body.Type.GetGenericArguments()[^1];
            JoinInfo join = Joins.First(f => f.EntityType == type && f.IsGroupJoin);
            join.JoinType = "LEFT JOIN";
            join.IsGroupJoin = false;

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;

            MemberExpression memberExpression = (MemberExpression)methodCallExpression.Arguments[0];
            Dictionary<string, Expression> result = [];

            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                if (tableColumn.Key.StartsWith(memberExpression.Member.Name))
                {
                    string path = tableColumn.Key[(memberExpression.Member.Name.Length + 1)..];
                    result.Add(path, tableColumn.Value);
                }
            }

            visitor.MethodArguments[resultSelector.Parameters[1]] = result;

            resultSelector = RowParameterExpander.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);
        }
        else
        {
            (Dictionary<string, Expression> newTableColumns, Type entityType, SQLiteExpression sql) = ResolveTable(selector.Body);

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

            resultSelector = RowParameterExpander.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);
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

    private static bool IsScalarBoxingToObject(Expression body)
    {
        return body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            && convert.Type == typeof(object)
            && convert.Operand.Type != typeof(object);
    }

    private static Expression? TryFlattenChainedSelectBody(LambdaExpression outer, LambdaExpression inner)
    {
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
