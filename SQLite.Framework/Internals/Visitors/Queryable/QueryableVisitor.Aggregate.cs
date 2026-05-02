namespace SQLite.Framework.Internals.Visitors;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitGroupFunction(MethodCallExpression node, string function)
    {
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
                Wheres.Add(sqlExpression);
                select = new SQLiteExpression(node.Arguments[0].Type, visitor.Counters.IdentifierIndex++, $"{function}(*)");
            }
            else
            {
                select = new SQLiteExpression(node.Arguments[1].Type, visitor.Counters.IdentifierIndex++, $"{function}({sqlExpression.Sql})", sqlExpression.Parameters);
            }
        }
        else if (function == "COUNT")
        {
            select = new SQLiteExpression(node.Arguments[0].Type, visitor.Counters.IdentifierIndex++, $"{function}(*)");
        }
        else if (Selects.Count == 1)
        {
            select = new SQLiteExpression(Selects[0].Type, visitor.Counters.IdentifierIndex++, $"{function}({Selects[0].Sql})", Selects[0].Parameters);
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

        return select;
    }

    private MethodCallExpression VisitGroupBy(MethodCallExpression node)
    {
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

        bool isMember = false;

        if (node.Arguments.Count == 3)
        {
            LambdaExpression resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);
            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

            if (resultSelector.Body is MemberExpression && TypeHelpers.IsSimple(resultSelector.Body.Type, database.Options))
            {
                isMember = true;
            }
        }

        Dictionary<string, Expression> newTableColumns = [];

        if (groupByExpression is NewExpression keyNew && keyNew.Members != null)
        {
            for (int i = 0; i < keyNew.Members.Count; i++)
            {
                string keyName = nameof(IGrouping<,>.Key) + "." + keyNew.Members[i].Name;
                newTableColumns[keyName] = keyNew.Arguments[i];
            }
        }
        else if (!isMember)
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
