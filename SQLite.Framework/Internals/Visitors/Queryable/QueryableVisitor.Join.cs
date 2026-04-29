namespace SQLite.Framework.Internals.Visitors;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitJoin(MethodCallExpression node, string joinType)
    {
        (Dictionary<string, Expression> newTableColumns, Type entityType, SQLiteExpression sql) = ResolveTable(node.Arguments[1]);

        LambdaExpression outerKey = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[2]);
        LambdaExpression innerKey = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[3]);
        LambdaExpression resultSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[4]);

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
                IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
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
                IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
            });
        }

        return sql;
    }
}
