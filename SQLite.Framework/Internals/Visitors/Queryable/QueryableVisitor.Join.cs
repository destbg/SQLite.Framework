namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitJoin(MethodCallExpression node, string joinType)
    {
        ThrowIfSetOperations(node.Method.Name);

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (joinType == "FULL OUTER JOIN" || joinType == "RIGHT JOIN")
        {
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_39, joinType);
        }
#endif

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

                sqlExpressions.Add(SQLiteExpression.Binary(typeof(bool), -1, "", outerAlias, " = ", innerAlias, "", combinedParameters));
            }

            SQLiteParameter[]? sqlParameters = ParameterHelpers.CombineParameters(sqlExpressions);
            SQLiteExpression[] onParts = sqlExpressions.ToArray();

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = joinType,
                Sql = sql,
                OnClause = SQLiteExpression.Variadic(typeof(bool), -1, "", onParts, " AND ", "", sqlParameters),
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
                OnClause = SQLiteExpression.Binary(typeof(bool), -1, "", outerAlias, " = ", innerAlias, "", parameters),
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
