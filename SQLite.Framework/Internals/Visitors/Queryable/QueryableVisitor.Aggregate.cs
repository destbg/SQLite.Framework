namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitGroupFunction(MethodCallExpression node, string function)
    {
        ThrowIfSetOperations(node.Method.Name);

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

                select = BuildScalarAggregate(function, node.Method.ReturnType, sqlExpression, distinctPrefix);
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
            select = BuildScalarAggregate(function, node.Method.ReturnType, Selects[0], distinctPrefix);
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

        return select;
    }

    private SQLiteExpression VisitGroupConcat(MethodCallExpression node)
    {
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
        if (function is "MAX" or "MIN" && TypeHelpers.UnsignedIntegerKey(innerExpr.Type) == typeof(ulong))
        {
            string nonMatchSide = function == "MAX" ? "< 0" : ">= 0";
            return SQLiteExpression.Multi(resultType, visitor.Counters.NextIdentifier(),
                [$"COALESCE({function}(CASE WHEN ", $" {nonMatchSide} THEN ", $" END), {function}(", "))"],
                [innerExpr, innerExpr, innerExpr],
                innerExpr.Parameters);
        }

        return function == "SUM"
            ? SQLiteExpression.Wrap(resultType, visitor.Counters.NextIdentifier(), $"COALESCE({function}({distinctPrefix}", innerExpr, "), 0)", innerExpr.Parameters)
            : SQLiteExpression.Wrap(resultType, visitor.Counters.NextIdentifier(), $"{function}({distinctPrefix}", innerExpr, ")", innerExpr.Parameters);
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
        Expression groupByExpression = visitor.Visit(lambda.Body);

        if (groupByExpression is SQLiteExpression keyExpression)
        {
            groupByExpression = visitor.CoalesceLiftedOrderComparison(lambda.Body, keyExpression);
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

        Dictionary<string, Expression> newTableColumns = [];

        if (groupByExpression is NewExpression keyNew && keyNew.Members != null)
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                newTableColumns[Constants.GroupingElementPrefix + tableColumn.Key] = tableColumn.Value;
            }

            for (int i = 0; i < keyNew.Members.Count; i++)
            {
                string keyName = nameof(IGrouping<,>.Key) + "." + keyNew.Members[i].Name;
                newTableColumns[keyName] = keyNew.Arguments[i];
            }
        }
        else if (!isScalarElement)
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
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

        visitor.TableColumns = newTableColumns;

        return node;
    }
}
