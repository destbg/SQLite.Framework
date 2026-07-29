namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitJoin(MethodCallExpression node, string joinType)
    {
        ThrowIfSetOperations(node.Method.Name);
        ComparerArgumentGuard.ThrowIfComparer(node);

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

        if (node.Method.Name != nameof(System.Linq.Queryable.GroupJoin))
        {
            if (joinType is "LEFT JOIN" or "FULL OUTER JOIN")
            {
                visitor.OptionalRowColumns.Add(newTableColumns);
            }

            if (joinType is "RIGHT JOIN" or "FULL OUTER JOIN")
            {
                visitor.OptionalRowColumns.Add(visitor.TableColumns);
            }
        }

        resultSelector = CommonHelpers.ExpandRowsInMethodCalls(resultSelector, visitor.MethodArguments.Keys);

        MarkGroupsDroppedByProjection(resultSelector);
        RemapGroupMemberPaths(resultSelector);

        bool isProjection = node.Method.Name != nameof(System.Linq.Queryable.GroupJoin)
            && resultSelector.Body is NewExpression or MemberInitExpression;

        bool isScalarSelector = node.Method.Name != nameof(System.Linq.Queryable.GroupJoin)
            && resultSelector.Body is not (NewExpression or MemberInitExpression or ParameterExpression or MemberExpression);

        if (isProjection || isScalarSelector)
        {
            JoinSelectExpression = null;
            Selects.Clear();
        }

        if ((isProjection || isScalarSelector) && database.Options.SelectMaterializers.Count > 0)
        {
            RawSelectSignature = SelectSignature.Compute(resultSelector.Body);
            LastSelectLambdaBody = resultSelector.Body;
        }

        visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

        if ((isProjection || isScalarSelector) && visitor.TableColumns.Values.Any(v => v is not SQLiteExpression))
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
            if (ContainsClientCall(decomposed))
            {
                LastSelectIsClient = true;
                ClientProjection = true;
            }
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

                SQLiteExpression outerAlias = visitor.PrepareKeyOperand(innerArgument, RequireJoinKey(visitor.Visit(innerArgument), innerArgument));
                SQLiteExpression innerAlias = visitor.PrepareKeyOperand(outerArgument, RequireJoinKey(visitor.Visit(outerArgument), outerArgument));
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
                IsGroupJoin = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin),
                GroupMemberPath = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin)
                    ? GetGroupMemberPath(resultSelector)
                    : null
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

            SQLiteExpression outerAlias = visitor.PrepareKeyOperand(outerBody, RequireJoinKey(visitor.Visit(outerBody), outerBody));
            SQLiteExpression innerAlias = visitor.PrepareKeyOperand(innerBody, RequireJoinKey(visitor.Visit(innerBody), innerBody));
            outerAlias = visitor.CoerceDayOfWeekOperand(outerBody, outerAlias, innerAlias);
            innerAlias = visitor.CoerceDayOfWeekOperand(innerBody, innerAlias, outerAlias);

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(outerAlias, innerAlias);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = joinType,
                Sql = sql,
                OnClause = SQLiteExpression.Binary(typeof(bool), -1, "", outerAlias, " = ", innerAlias, "", parameters),
                IsGroupJoin = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin),
                GroupMemberPath = node.Method.Name == nameof(System.Linq.Queryable.GroupJoin)
                    ? GetGroupMemberPath(resultSelector)
                    : null
            });
        }

        return sql;
    }

    private void RemapGroupMemberPaths(LambdaExpression resultSelector)
    {
        List<JoinInfo> openGroups = Joins
            .Where(f => f is { IsGroupJoin: true, GroupFlattened: false, GroupDropped: false, GroupMemberPath: not null })
            .ToList();
        if (openGroups.Count == 0)
        {
            return;
        }

        List<(JoinInfo Group, string? NewPath)> updates = [];
        foreach (JoinInfo group in openGroups)
        {
            updates.Add((group, FindMemberPath(resultSelector.Body, resultSelector.Parameters[0], group.GroupMemberPath!, string.Empty)));
        }

        foreach ((JoinInfo group, string? newPath) in updates)
        {
            if (newPath != null)
            {
                group.GroupMemberPath = newPath;
            }
        }
    }

    private void MarkGroupsDroppedByProjection(LambdaExpression resultSelector)
    {
        List<JoinInfo> openGroups = Joins.Where(f => f is { IsGroupJoin: true, GroupFlattened: false, GroupDropped: false }).ToList();
        if (openGroups.Count == 0)
        {
            return;
        }

        CarriedPathCollector collector = new(resultSelector.Parameters[0]);
        collector.Visit(resultSelector.Body);

        foreach (JoinInfo group in openGroups)
        {
            if (group.GroupMemberPath is { } path && !collector.Carries(path))
            {
                group.GroupDropped = true;
            }
        }
    }

    private static SQLiteExpression RequireJoinKey(Expression? translated, Expression original)
    {
        if (translated is SQLiteExpression sqlExpression)
        {
            return sqlExpression;
        }

        throw new NotSupportedException(
            $"The join key '{original}' cannot be translated to SQL. " +
            "A join key runs inside the database, so it cannot call methods that run in memory.");
    }

    private static string? FindMemberPath(Expression body, ParameterExpression parameter, string sourcePath, string prefix)
    {
        if (body is NewExpression { Members: not null } newExpression)
        {
            for (int i = 0; i < newExpression.Arguments.Count; i++)
            {
                if (FindMemberPath(newExpression.Arguments[i], parameter, sourcePath, Combine(prefix, newExpression.Members[i].Name)) is { } nested)
                {
                    return nested;
                }
            }

            return null;
        }

        if (body is MemberInitExpression memberInit)
        {
            foreach (MemberAssignment assignment in memberInit.Bindings.OfType<MemberAssignment>())
            {
                if (FindMemberPath(assignment.Expression, parameter, sourcePath, Combine(prefix, assignment.Member.Name)) is { } nested)
                {
                    return nested;
                }
            }

            return null;
        }

        if (prefix.Length == 0)
        {
            return null;
        }

        (string bodyPath, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(body);
        if (pe != parameter)
        {
            return null;
        }

        if (bodyPath == sourcePath)
        {
            return prefix;
        }

        if (bodyPath.Length == 0 || sourcePath.StartsWith(bodyPath + ".", StringComparison.Ordinal))
        {
            string suffix = bodyPath.Length == 0 ? sourcePath : sourcePath[(bodyPath.Length + 1)..];
            return prefix + "." + suffix;
        }

        return null;
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

    private static string? GetGroupMemberPath(LambdaExpression resultSelector)
    {
        return FindGroupPath(resultSelector.Body, resultSelector.Parameters[1], string.Empty);
    }

    private static string? FindGroupPath(Expression body, ParameterExpression group, string prefix)
    {
        if (body == group)
        {
            return prefix.Length == 0 ? null : prefix;
        }

        if (body is NewExpression newExpression && newExpression.Members != null)
        {
            for (int i = 0; i < newExpression.Arguments.Count; i++)
            {
                string path = Combine(prefix, newExpression.Members[i].Name);
                if (FindGroupPath(newExpression.Arguments[i], group, path) is { } found)
                {
                    return found;
                }
            }
        }

        if (body is MemberInitExpression memberInit)
        {
            foreach (MemberAssignment assignment in memberInit.Bindings.OfType<MemberAssignment>())
            {
                string path = Combine(prefix, assignment.Member.Name);
                if (FindGroupPath(assignment.Expression, group, path) is { } found)
                {
                    return found;
                }
            }

            return FindGroupPath(memberInit.NewExpression, group, prefix);
        }

        return null;
    }

    private static string Combine(string prefix, string name)
    {
        return prefix.Length == 0 ? name : prefix + "." + name;
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
