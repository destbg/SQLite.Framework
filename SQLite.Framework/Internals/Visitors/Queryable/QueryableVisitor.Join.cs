namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitJoin(MethodCallExpression node, string joinType)
    {
        (Dictionary<string, Expression> newTableColumns, Type entityType, SQLiteExpression sql) = ResolveTable(node.Arguments[1]);

        LambdaExpression outerKey = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);
        LambdaExpression innerKey = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[3]);
        LambdaExpression resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[4]);

        if (node.Method.Name == nameof(System.Linq.Queryable.GroupJoin))
        {
            EnsureGroupJoinResultSelectorIsPassthrough(resultSelector);
        }

        visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
        visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

        resultSelector = RowParameterExpander.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);
        visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

        visitor.MethodArguments[innerKey.Parameters[0]] = newTableColumns;

        if (outerKey.Body is NewExpression outerNewExpression)
        {
            NewExpression innerNewExpression = (NewExpression)innerKey.Body;

            List<SQLiteExpression> sqlExpressions = [];

            for (int i = 0; i < innerNewExpression.Arguments.Count; i++)
            {
                Expression innerArgument = innerNewExpression.Arguments[i];
                Expression outerArgument = outerNewExpression.Arguments[i];

                SQLiteExpression outerAlias = (SQLiteExpression)visitor.Visit(innerArgument);
                SQLiteExpression innerAlias = (SQLiteExpression)visitor.Visit(outerArgument);

                SQLiteParameter[]? combinedParameters = ParameterHelpers.CombineParameters(outerAlias, innerAlias);

                string comparison = $"{outerAlias.Sql} = {innerAlias.Sql}";
                sqlExpressions.Add(new SQLiteExpression(typeof(bool), -1, comparison, combinedParameters));
            }

            string onClause = string.Join(" AND ", sqlExpressions.Select(f => f.Sql));
            SQLiteParameter[]? sqlParameters = ParameterHelpers.CombineParameters(sqlExpressions);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = joinType,
                Sql = sql,
                OnClause = new SQLiteExpression(typeof(bool), -1, onClause, sqlParameters),
                IsGroupJoin = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin)
            });
        }
        else
        {
            SQLiteExpression outerAlias = (SQLiteExpression)visitor.Visit(outerKey.Body);
            SQLiteExpression innerAlias = (SQLiteExpression)visitor.Visit(innerKey.Body);

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(outerAlias, innerAlias);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = joinType,
                Sql = sql,
                OnClause = new SQLiteExpression(typeof(bool), -1, $"{outerAlias.Sql} = {innerAlias.Sql}", parameters),
                IsGroupJoin = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin)
            });
        }

        return sql;
    }

    private static void EnsureGroupJoinResultSelectorIsPassthrough(LambdaExpression resultSelector)
    {
        ParameterExpression group = resultSelector.Parameters[1];
        GroupSequenceUsageWalker walker = new(group);
        walker.Visit(resultSelector.Body);

        if (walker.UsesGroupAsSequence)
        {
            throw new NotSupportedException(
                "GroupJoin (the LINQ 'into <name>' syntax) is only supported when followed by " +
                "'from x in <name>.DefaultIfEmpty()' to flatten the group into a LEFT JOIN. " +
                "Calling sequence methods on the group directly (for example 'bg.Count()' or " +
                "'bg.Sum(...)' inside the projection) is not supported. Rewrite the projection " +
                "as a correlated subquery, for example: " +
                "'select new { a.Id, Count = db.Table<Book>().Count(b => b.AuthorId == a.Id) }'.");
        }
    }
}
