namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private Expression VisitGroupFunction(MethodCallExpression node, string function)
    {
        ThrowIfSetOperations(node.Method.Name);

        if (LastSelectIsClient)
        {
            if (function != "COUNT" || node.Arguments.Count != 1)
            {
                throw new NotSupportedException(
                    $"{node.Method.Name} after a projection that runs in memory is not supported, because SQLite " +
                    "cannot aggregate a value the database never computes. " +
                    "Materialize the values with ToList and aggregate in memory.");
            }

            if (ClientTake != null || ClientSkip != null || IsDistinct || Take != null || Skip != null)
            {
                ClientCount = true;
                return node;
            }
        }

        if (Selects.Count == 0 && visitor.TableColumns.Count == 1)
        {
            Selects.Add((SQLiteExpression)visitor.TableColumns.Values.Single());
        }

        bool applyDistinct = IsDistinct && function != "MAX" && function != "MIN";
        string distinctPrefix = applyDistinct ? "DISTINCT " : string.Empty;

        SQLiteExpression select;
        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            ThrowIfGroupJoinGroupPredicate(lambda.Body);
            if (function == "COUNT")
            {
                ThrowIfWindowPredicate(lambda.Body);
            }

            Expression expression = visitor.Visit(lambda.Body);

            if (expression is not SQLiteExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported {function} expression {lambda.Body}");
            }

            if (function == "COUNT")
            {
                if (applyDistinct)
                {
                    ThrowOnMultiColumnDistinct(node);
                    select = NullAwareDistinctCount(node.Arguments[0].Type, Selects[0]);
                    Wheres.Add(sqlExpression);
                }
                else
                {
                    Wheres.Add(sqlExpression);
                    select = SQLiteExpression.Leaf(node.Arguments[0].Type, visitor.Counters.NextIdentifier(), "COUNT(*)");
                }
            }
            else
            {
                if (applyDistinct)
                {
                    ThrowOnMultiColumnDistinct(node);

                    throw new NotSupportedException(
                        $"{node.Method.Name} with a selector after Distinct() is not supported, because the DISTINCT " +
                        "would apply to the selector result instead of to the source rows.");
                }

                SQLiteExpression aggregateTarget = visitor.CoalesceLiftedOrderComparison(lambda.Body, sqlExpression);
                if (function is "MIN" or "MAX")
                {
                    aggregateTarget = visitor.CastTextDecimalForOrdering(aggregateTarget);
                }

                select = BuildScalarAggregate(function, node.Method.ReturnType, aggregateTarget, distinctPrefix);
            }
        }
        else if (function == "COUNT")
        {
            if (applyDistinct)
            {
                ThrowOnMultiColumnDistinct(node);
                select = NullAwareDistinctCount(node.Arguments[0].Type, Selects[0]);
            }
            else
            {
                select = SQLiteExpression.Leaf(node.Arguments[0].Type, visitor.Counters.NextIdentifier(), "COUNT(*)");
            }
        }
        else if (Selects.Count == 1)
        {
            SQLiteExpression aggregateTarget = Selects[0];
            if (function is "MIN" or "MAX"
                && database.Options.DecimalStorage == DecimalStorageMode.Text
                && (Nullable.GetUnderlyingType(node.Method.ReturnType) ?? node.Method.ReturnType) == typeof(decimal))
            {
                aggregateTarget = visitor.InternDecimalCast(aggregateTarget);
            }

            select = BuildScalarAggregate(function, node.Method.ReturnType, aggregateTarget, distinctPrefix);
        }
        else
        {
            string methodName = node.Method.Name;
            throw new NotSupportedException(
                $"{methodName} requires a single scalar column. Use a selector ('.{methodName}(x => x.Column)') " +
                $"or project to one column first ('.Select(x => x.Column).{methodName}()').");
        }

        Selects.Clear();
        Selects.Add(select);

        IsDistinct = false;
        SuppressSelectMaterializer = true;

        return select;
    }

    private SQLiteExpression VisitGroupConcat(MethodCallExpression node)
    {
        if (LastSelectIsClient)
        {
            throw new NotSupportedException(
                "string.Join over a projection that runs in memory is not supported, because SQLite " +
                "cannot concatenate a value the database never computes. " +
                "Materialize the values with ToList and call string.Join in memory.");
        }

        if (Take != null || Skip != null)
        {
            throw new NotSupportedException(
                "string.Join over an IQueryable does not support Take or Skip on the source. " +
                "Materialize the limited rows first with ToList and call string.Join in memory.");
        }

        ThrowIfSetOperations(node.Method.Name);

        if (Reverse)
        {
            throw new NotSupportedException(
                "string.Join over a Reverse() queryable is not supported. " +
                "Use OrderByDescending instead of OrderBy().Reverse() so SQLite can order the values.");
        }

        if (Selects.Count != 1)
        {
            throw new NotSupportedException(
                "string.Join over an IQueryable requires a single-column projection. " +
                "Project to one column first (for example 'string.Join(\", \", q.Select(x => x.Name))').");
        }

        if (IsDistinct)
        {
            throw new NotSupportedException(
                "string.Join over a Distinct() queryable is not supported. " +
                "SQLite's group_concat aggregate rejects a custom separator when DISTINCT is used. " +
                "Materialize with ToList() and call string.Join in memory, " +
                "or drop the Distinct() and let group_concat keep duplicates.");
        }

        SQLiteExpression separatorExpression = (SQLiteExpression)visitor.Visit(node.Arguments[1]);
        SQLiteExpression innerExpression = Selects[0];
        SQLiteExpression select;

#if !SQLITECIPHER
        if (OrderBys.Count > 0)
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_44, "ORDER BY inside group_concat");
#endif
            int orderCount = OrderBys.Count;
            SQLiteExpression[] children = new SQLiteExpression[2 + orderCount];
            children[0] = innerExpression;
            children[1] = separatorExpression;
            for (int i = 0; i < orderCount; i++)
            {
                children[2 + i] = OrderBys[i];
            }

            string[] parts = new string[3 + orderCount];
            parts[0] = "COALESCE(group_concat(COALESCE(";
            parts[1] = ", ''), ";
            parts[2] = " ORDER BY ";
            for (int i = 0; i < orderCount - 1; i++)
            {
                parts[3 + i] = ", ";
            }
            parts[2 + orderCount] = "), '')";

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(children);
            select = SQLiteExpression.Multi(
                typeof(string),
                visitor.Counters.NextIdentifier(),
                parts,
                children,
                parameters);

            OrderBys.Clear();
        }
        else
#endif
        {
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(innerExpression, separatorExpression);
            select = SQLiteExpression.Binary(
                typeof(string),
                visitor.Counters.NextIdentifier(),
                "COALESCE(group_concat(COALESCE(",
                innerExpression,
                ", ''), ",
                separatorExpression,
                "), '')",
                parameters);
        }

        Selects.Clear();
        Selects.Add(select);

        return select;
    }

    private SQLiteExpression VisitTotal(MethodCallExpression node)
    {
        if (Take != null || Skip != null)
        {
            throw new NotSupportedException(
                "Total over an IQueryable does not support Take or Skip on the source. " +
                "Materialize the limited rows first with ToList and call SQLiteFunctions.Total over them, " +
                "or move the limit inside a CTE.");
        }

        ThrowIfSetOperations(node.Method.Name);

        if (IsDistinct)
        {
            throw new NotSupportedException(
                "Total over a Distinct() queryable is not supported. " +
                "Materialize with ToList() and total in memory, " +
                "or drop the Distinct() and let total() keep duplicates.");
        }

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        Expression expression = visitor.Visit(lambda.Body);

        if (expression is not SQLiteExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported Total expression {lambda.Body}");
        }

        SQLiteExpression select = SQLiteExpression.Wrap(
            typeof(double),
            visitor.Counters.NextIdentifier(),
            "total(",
            sqlExpression,
            ")",
            sqlExpression.Parameters);

        Selects.Clear();
        Selects.Add(select);

        return select;
    }

    private SQLiteExpression BuildScalarAggregate(string function, Type resultType, SQLiteExpression innerExpr, string distinctPrefix)
    {
        if (function is "MAX" or "MIN"
            && (TypeHelpers.UnsignedIntegerKey(innerExpr.Type) == typeof(ulong) || TypeHelpers.UnsignedIntegerKey(resultType) == typeof(ulong)))
        {
            string nonMatchSide = function == "MAX" ? "< 0" : ">= 0";
            return SQLiteExpression.Multi(resultType, visitor.Counters.NextIdentifier(),
                [$"COALESCE({function}(CASE WHEN ", $" {nonMatchSide} THEN ", $" END), {function}(", "))"],
                [innerExpr, innerExpr, innerExpr],
                innerExpr.Parameters);
        }

        SQLiteExpression aggregate = function == "SUM"
            ? SQLiteExpression.Wrap(resultType, visitor.Counters.NextIdentifier(), $"COALESCE({function}({distinctPrefix}", innerExpr, "), 0)", innerExpr.Parameters)
            : SQLiteExpression.Wrap(resultType, visitor.Counters.NextIdentifier(), $"{function}({distinctPrefix}", innerExpr, ")", innerExpr.Parameters);
        if (function is "MAX" or "MIN" && innerExpr.IsDayOfWeekInteger)
        {
            aggregate.WithDayOfWeekInteger();
        }

        return aggregate;
    }

    private SQLiteExpression NullAwareDistinctCount(Type type, SQLiteExpression column)
    {
        return SQLiteExpression.Multi(
            type,
            visitor.Counters.NextIdentifier(),
            ["(COUNT(DISTINCT ", ") + (CASE WHEN COUNT(*) > COUNT(", ") THEN 1 ELSE 0 END))"],
            [column, column],
            column.Parameters);
    }

    private void ThrowOnMultiColumnDistinct(MethodCallExpression node)
    {
        if (Selects.Count != 1)
        {
            string methodName = node.Method.Name;
            throw new NotSupportedException(
                $"{methodName} after Distinct requires a single-column projection. " +
                $"Project first (e.g., '.Select(x => x.Column).Distinct().{methodName}()') " +
                $"or materialize with '.ToList()' and call '.Distinct().{methodName}()' in memory.");
        }
    }

    private MethodCallExpression VisitGroupBy(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);
        ComparerArgumentGuard.ThrowIfComparer(node);

        if (GroupBys.Count != 0)
        {
            throw new NotSupportedException(
                "Only a single GroupBy is supported per query. " +
                "Combine both groupings into one projection (e.g. `.GroupBy(x => new { x.A, x.B }).Select(g => ...)`), " +
                "or materialize the first result with `.ToListAsync()` and perform the second GroupBy client-side.");
        }

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);

        SelectVisitor groupByVisitor = new(GroupBys);
        Expression keyBody = RewriteTupleCreateKey(lambda.Body);

        if (WindowCallDetector.Contains(keyBody))
        {
            throw new NotSupportedException(
                "A window function cannot be used in a GroupBy key, because SQL groups rows before window functions run.");
        }

        if (keyBody is ParameterExpression identityKeyParam
            && !TypeHelpers.IsSimple(identityKeyParam.Type, database.Options)
            && visitor.MethodArguments.TryGetValue(identityKeyParam, out Dictionary<string, Expression>? identityKeyColumns))
        {
            foreach (Expression identityColumn in identityKeyColumns.Values)
            {
                if (identityColumn is not SQLiteExpression identityKeyColumn)
                {
                    throw new NotSupportedException(
                        "GroupBy over the whole row is not supported when the row holds a value computed in memory.");
                }

                GroupBys.Add(identityKeyColumn);
            }

            return FinishGroupBy(node, lambda, GroupBys[0]);
        }

        Expression groupByExpression = visitor.Visit(keyBody);

        if (groupByExpression is SQLiteExpression keyExpression)
        {
            groupByExpression = visitor.CoalesceLiftedOrderComparison(keyBody, keyExpression);
        }
        else if (groupByExpression is NewExpression translatedKey)
        {
            Expression[] coalesced = new Expression[translatedKey.Arguments.Count];
            for (int i = 0; i < translatedKey.Arguments.Count; i++)
            {
                Expression original = keyBody is NewExpression originalKey ? originalKey.Arguments[i] : translatedKey.Arguments[i];
                coalesced[i] = CoalesceNestedGroupKey(original, translatedKey.Arguments[i]);
            }

            groupByExpression = translatedKey.Update(coalesced);
        }

        if (groupByExpression is not SQLiteExpression && groupByExpression is not NewExpression)
        {
            throw new NotSupportedException(
                $"Could not translate the GroupBy key selector `{lambda}` to SQL. " +
                "The key selector must reference columns of the table (e.g. `.GroupBy(x => x.CategoryId)` " +
                "or `.GroupBy(x => new {{ x.A, x.B }})`).");
        }

        groupByVisitor.Visit(groupByExpression);

        if (GroupBys.Count == 0)
        {
            throw new NotSupportedException(
                $"Could not translate the GroupBy key selector `{lambda}` to SQL. " +
                "The key selector must reference columns of the table (e.g. `.GroupBy(x => x.CategoryId)` " +
                "or `.GroupBy(x => new {{ x.A, x.B }})`).");
        }

        return FinishGroupBy(node, lambda, groupByExpression);
    }

    private MethodCallExpression FinishGroupBy(MethodCallExpression node, LambdaExpression lambda, Expression groupByExpression)
    {
        bool isScalarElement = false;

        if (node.Arguments.Count == 3)
        {
            LambdaExpression resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);
            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

            if (TypeHelpers.IsSimple(resultSelector.Body.Type, database.Options))
            {
                isScalarElement = true;
            }
        }
        else if (TypeHelpers.IsSimple(lambda.Parameters[0].Type, database.Options))
        {
            isScalarElement = true;
        }

        Dictionary<string, Expression> newTableColumns = [];

        if (groupByExpression is NewExpression keyNew)
        {
            if (isScalarElement)
            {
                newTableColumns[string.Empty] = visitor.TableColumns.Single().Value;
            }
            else
            {
                foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
                {
                    newTableColumns[Constants.GroupingElementPrefix + tableColumn.Key] = tableColumn.Value;
                }
            }

            HashSet<string> constructedKeyPaths = [];
            if (ResolveGroupKeyMemberNames(keyNew) is { } keyMemberNames)
            {
                for (int i = 0; i < keyMemberNames.Count; i++)
                {
                    string keyName = nameof(IGrouping<,>.Key) + "." + keyMemberNames[i];
                    AddGroupKeyColumns(newTableColumns, constructedKeyPaths, keyName, keyNew.Arguments[i]);
                }
            }
            else
            {
                constructedKeyPaths.Add(nameof(IGrouping<,>.Key));
                newTableColumns[nameof(IGrouping<,>.Key)] = keyNew;
            }

            if (constructedKeyPaths.Count > 0)
            {
                visitor.ConstructedProjectionPaths[newTableColumns] = constructedKeyPaths;
            }
        }
        else if (!isScalarElement)
        {
            bool keyIsSimple = TypeHelpers.IsSimple(lambda.Body.Type, database.Options);
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                if (keyIsSimple)
                {
                    newTableColumns[Constants.GroupingElementPrefix + tableColumn.Key] = tableColumn.Value;
                    continue;
                }

                string[] split = tableColumn.Key.Split('.');
                string key = string.Join('.', [nameof(IGrouping<,>.Key), .. split]);

                newTableColumns[key] = tableColumn.Value;
            }
            newTableColumns[nameof(IGrouping<,>.Key)] = GroupBys[0];
        }
        else
        {
            newTableColumns[string.Empty] = visitor.TableColumns.Single().Value;
            newTableColumns[nameof(IGrouping<,>.Key)] = GroupBys[0];
        }

        if (visitor.OptionalRowPaths.TryGetValue(visitor.TableColumns, out HashSet<string>? optionalPaths))
        {
            HashSet<string> groupedOptionalPaths = new(StringComparer.Ordinal);
            foreach (string optionalPath in optionalPaths)
            {
                groupedOptionalPaths.Add(Constants.GroupingElementPrefix + optionalPath);
            }

            visitor.OptionalRowPaths[newTableColumns] = groupedOptionalPaths;
        }

        if (visitor.ConstructedProjectionPaths.TryGetValue(visitor.TableColumns, out HashSet<string>? elementConstructedPaths))
        {
            HashSet<string> groupedConstructedPaths = visitor.ConstructedProjectionPaths.TryGetValue(newTableColumns, out HashSet<string>? keyPaths)
                ? keyPaths
                : new HashSet<string>(StringComparer.Ordinal);
            foreach (string constructedPath in elementConstructedPaths)
            {
                groupedConstructedPaths.Add(Constants.GroupingElementPrefix + constructedPath);
            }

            visitor.ConstructedProjectionPaths[newTableColumns] = groupedConstructedPaths;
        }

        visitor.TableColumns = newTableColumns;

        return node;
    }

    private Expression CoalesceNestedGroupKey(Expression original, Expression translated)
    {
        if (translated is SQLiteExpression sqlExpression)
        {
            return visitor.CoalesceLiftedOrderComparison(original, sqlExpression);
        }

        if (translated is MemberInitExpression memberInit && original is MemberInitExpression originalInit)
        {
            List<MemberBinding> bindings = new(memberInit.Bindings.Count);
            for (int i = 0; i < memberInit.Bindings.Count; i++)
            {
                if (memberInit.Bindings[i] is MemberAssignment assignment
                    && originalInit.Bindings[i] is MemberAssignment originalAssignment)
                {
                    bindings.Add(Expression.Bind(assignment.Member, CoalesceNestedGroupKey(originalAssignment.Expression, assignment.Expression)));
                }
                else
                {
                    bindings.Add(memberInit.Bindings[i]);
                }
            }

            return memberInit.Update(memberInit.NewExpression, bindings);
        }

        if (translated is NewExpression newExpression)
        {
            NewExpression originalNew = (NewExpression)original;
            Expression[] arguments = new Expression[newExpression.Arguments.Count];
            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i] = CoalesceNestedGroupKey(originalNew.Arguments[i], newExpression.Arguments[i]);
            }

            return newExpression.Update(arguments);
        }

        return translated;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Builds an expression tree for the translator.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Tuple constructors are preserved with the tuple type.")]
    private static Expression RewriteTupleCreateKey(Expression body)
    {
        if (body is MethodCallExpression { Method.Name: "Create", Object: null } createCall
            && (createCall.Method.DeclaringType == typeof(ValueTuple) || createCall.Method.DeclaringType == typeof(Tuple))
            && createCall.Type.GetConstructor(createCall.Arguments.Select(a => a.Type).ToArray()) is { } tupleConstructor)
        {
            return Expression.New(tupleConstructor, createCall.Arguments);
        }

        return body;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Group key types are rooted by the user query.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Group key types are rooted by the user query.")]
    private static List<string>? ResolveGroupKeyMemberNames(NewExpression keyNew)
    {
        if (keyNew.Members != null)
        {
            return keyNew.Members.Select(m => m.Name).ToList();
        }

        if (keyNew.Constructor == null || keyNew.Arguments.Count == 0)
        {
            return null;
        }

        bool identityType = TypeHelpers.HasPositionalIdentityMembers(keyNew.Type);
        ParameterInfo[] parameters = keyNew.Constructor.GetParameters();
        List<string> names = new(parameters.Length);
        foreach (ParameterInfo parameter in parameters)
        {
            PropertyInfo? property = keyNew.Type.GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && (identityType || !property.CanWrite))
            {
                names.Add(property.Name);
                continue;
            }

            FieldInfo? field = identityType
                ? keyNew.Type.GetField(parameter.Name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                : null;
            if (field != null)
            {
                names.Add(field.Name);
                continue;
            }

            return null;
        }

        return names;
    }

    private static void AddGroupKeyColumns(Dictionary<string, Expression> columns, HashSet<string> constructedPaths, string path, Expression value)
    {
        if (value is MemberInitExpression memberInit)
        {
            constructedPaths.Add(path);
            foreach (MemberAssignment assignment in memberInit.Bindings.OfType<MemberAssignment>())
            {
                AddGroupKeyColumns(columns, constructedPaths, path + "." + assignment.Member.Name, assignment.Expression);
            }

            return;
        }

        if (value is NewExpression { Members: not null } nested)
        {
            constructedPaths.Add(path);
            for (int i = 0; i < nested.Members.Count; i++)
            {
                AddGroupKeyColumns(columns, constructedPaths, path + "." + nested.Members[i].Name, nested.Arguments[i]);
            }

            return;
        }

        columns[path] = value;
    }
}
