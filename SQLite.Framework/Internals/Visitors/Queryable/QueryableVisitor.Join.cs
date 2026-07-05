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

        resultSelector = CommonHelpers.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);

        bool isProjection = node.Method.Name != nameof(System.Linq.Queryable.GroupJoin)
            && resultSelector.Body is NewExpression or MemberInitExpression;

        if (isProjection && database.Options.SelectMaterializers.Count > 0)
        {
            RawSelectSignature = SelectSignature.Compute(resultSelector.Body);
            LastSelectLambdaBody = resultSelector.Body;
        }

        visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

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

        visitor.MethodArguments[innerKey.Parameters[0]] = newTableColumns;

        if (outerKey.Body is NewExpression outerNewExpression)
        {
            NewExpression innerNewExpression = (NewExpression)innerKey.Body;

            List<SQLiteExpression> sqlExpressions = [];

            for (int i = 0; i < innerNewExpression.Arguments.Count; i++)
            {
                Expression innerArgument = innerNewExpression.Arguments[i];
                Expression outerArgument = outerNewExpression.Arguments[i];

                if (DayOfWeekHelpers.IsComputedDayOfWeek(innerArgument) || DayOfWeekHelpers.IsComputedDayOfWeek(outerArgument))
                {
                    innerArgument = DayOfWeekHelpers.ConvertOperandToInt(visitor.Database.Options, innerArgument);
                    outerArgument = DayOfWeekHelpers.ConvertOperandToInt(visitor.Database.Options, outerArgument);
                }

                SQLiteExpression outerAlias = visitor.PrepareKeyOperand(innerArgument, (SQLiteExpression)visitor.Visit(innerArgument));
                SQLiteExpression innerAlias = visitor.PrepareKeyOperand(outerArgument, (SQLiteExpression)visitor.Visit(outerArgument));
                outerAlias = visitor.CoerceDayOfWeekOperand(innerArgument, outerAlias, innerAlias);
                innerAlias = visitor.CoerceDayOfWeekOperand(outerArgument, innerAlias, outerAlias);

                SQLiteParameter[]? combinedParameters = ParameterHelpers.CombineParameters(outerAlias, innerAlias);

                string keyOp = CompositeJoinKeyOperator(outerArgument.Type, innerArgument.Type);
                sqlExpressions.Add(SQLiteExpression.Binary(typeof(bool), -1, "", outerAlias, keyOp, innerAlias, "", combinedParameters));
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
            Expression outerBody = outerKey.Body;
            Expression innerBody = innerKey.Body;

            if (DayOfWeekHelpers.IsComputedDayOfWeek(outerBody) || DayOfWeekHelpers.IsComputedDayOfWeek(innerBody))
            {
                outerBody = DayOfWeekHelpers.ConvertOperandToInt(visitor.Database.Options, outerBody);
                innerBody = DayOfWeekHelpers.ConvertOperandToInt(visitor.Database.Options, innerBody);
            }

            SQLiteExpression outerAlias = visitor.PrepareKeyOperand(outerBody, (SQLiteExpression)visitor.Visit(outerBody));
            SQLiteExpression innerAlias = visitor.PrepareKeyOperand(innerBody, (SQLiteExpression)visitor.Visit(innerBody));
            outerAlias = visitor.CoerceDayOfWeekOperand(outerBody, outerAlias, innerAlias);
            innerAlias = visitor.CoerceDayOfWeekOperand(innerBody, innerAlias, outerAlias);

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

    private static Dictionary<string, Expression> DecomposeJoinProjectionColumns(NewExpression newExpression)
    {
        Dictionary<string, Expression> columns = new();

        for (int i = 0; i < newExpression.Arguments.Count; i++)
        {
            columns[newExpression.Members![i].Name] = newExpression.Arguments[i];
        }

        return columns;
    }

    private static string CompositeJoinKeyOperator(Type outerType, Type innerType)
    {
        bool nullable = IsNullableKeyComponent(outerType) || IsNullableKeyComponent(innerType);
        return nullable ? " IS " : " = ";
    }

    private static bool IsNullableKeyComponent(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
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
