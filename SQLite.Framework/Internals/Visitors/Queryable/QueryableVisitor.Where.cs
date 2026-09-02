namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private Expression VisitWhere(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        ThrowIfGroupJoinGroupPredicate(lambda.Body);
        if (CommonHelpers.ContainsWindowCall(lambda.Body))
        {
            throw new NotSupportedException(
                "A window function in Where needs a one-parameter predicate that can be moved to an outer query.");
        }

        bool previousConstantMethodFoldingAllowed = visitor.ConstantMethodFoldingAllowed;
        visitor.ConstantMethodFoldingAllowed = true;
        Expression result;
        try
        {
            result = visitor.Visit(lambda.Body);
        }
        finally
        {
            visitor.ConstantMethodFoldingAllowed = previousConstantMethodFoldingAllowed;
        }

        if (result is not SQLiteExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
        }

        if (GroupBys.Count != 0)
        {
            Havings.Add(sqlExpression);
        }
        else
        {
            Wheres.Add(sqlExpression);
        }

        return result;
    }

    private MethodCallExpression VisitContains(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);
        ComparerArgumentGuard.ThrowIfComparer(node);

        if (visitor.TableColumns.Count != 1)
        {
            throw new NotSupportedException("Contains is only supported for a single column.");
        }

        ResolvedModel resolved = visitor.ResolveExpression(node.Arguments[1]);
        SQLiteExpression sqlExpression;

        if (resolved.IsConstant)
        {
            if (resolved.Constant != null && !TypeHelpers.IsSimple(resolved.Constant.GetType(), database.Options))
            {
                throw new NotSupportedException("Contains is only supported for a single column.");
            }

            sqlExpression = resolved.SQLiteExpression!;
        }
        else if (resolved.SQLiteExpression != null)
        {
            sqlExpression = resolved.SQLiteExpression;
        }
        else
        {
            throw new NotSupportedException(
                $"Contains needs a value that SQLite can translate. The value expression is {node.Arguments[1].GetType().Name}. " +
                "Read the value into a local variable or call Contains after AsEnumerable.");
        }

        if (visitor.TableColumns.Values.First() is not SQLiteExpression columnExpr)
        {
            string advice = IsInnerQuery
                ? "Read the projected values into a list first and call Contains on the list."
                : "Call AsEnumerable before Contains to check the projected values in memory.";

            throw new NotSupportedException(
                "Contains after a projection that runs in memory is not supported, because SQL cannot compare a " +
                $"value the database never computes. {advice}");
        }

        if (!IsInnerQuery)
        {
            List<SQLiteExpression> sink = GroupBys.Count != 0 ? Havings : Wheres;

            if (resolved is { IsConstant: true, Constant: null })
            {
                sink.Add(SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", columnExpr, " IS NULL", columnExpr.Parameters));
            }
            else
            {
                sqlExpression = visitor.CoerceDayOfWeekOperand(node.Arguments[1], sqlExpression, columnExpr);
                sink.Add(SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "", columnExpr, " = ", sqlExpression, "", ParameterHelpers.CombineParameters(columnExpr, sqlExpression)));
            }

            IsAny = true;
        }

        return node;
    }

    private MethodCallExpression VisitScalar(MethodCallExpression node)
    {
        CheckWhereArgument(node);
        ThrowIfReverse(node.Method.Name);
        IsRowSelector = true;

        bool clientSide = IsDistinct && LastSelectIsClient;
        if (node.Method.Name is nameof(System.Linq.Queryable.Single) or nameof(System.Linq.Queryable.SingleOrDefault))
        {
            if (clientSide)
            {
                ClientTake = ClientTake.HasValue ? Math.Min(ClientTake.Value, 2) : 2;
            }
            else
            {
                Take = Take.HasValue ? Math.Min(Take.Value, 2) : 2;
            }

            ThrowOnMoreThanOne = true;
        }
        else if (clientSide)
        {
            ClientTake = ClientTake.HasValue ? Math.Min(ClientTake.Value, 1) : 1;
        }
        else
        {
            Take = Take.HasValue ? Math.Min(Take.Value, 1) : 1;
        }

        if (node.Method.Name is nameof(System.Linq.Queryable.First) or nameof(System.Linq.Queryable.Single))
        {
            ThrowOnEmpty = true;
        }

        return node;
    }

    private MethodCallExpression VisitBoolean(MethodCallExpression node)
    {
        if (IsDistinct && LastSelectIsClient
            && node.Method.Name == nameof(System.Linq.Queryable.Any)
            && node.Arguments.Count == 1)
        {
            ClientCount = true;
            return node;
        }

        IsAny = node.Method.Name == nameof(System.Linq.Queryable.Any);
        IsAll = node.Method.Name == nameof(System.Linq.Queryable.All);
        SuppressSelectMaterializer = true;

        if (IsAll && node.Arguments.Count >= 2)
        {
            ThrowIfSetOperations(node.Method.Name);

            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            ThrowIfGroupJoinGroupPredicate(lambda.Body);
            bool previousFtsMatchAsSubquery = visitor.FtsMatchAsSubquery;
            visitor.FtsMatchAsSubquery = true;
            Expression result = visitor.Visit(lambda.Body);
            visitor.FtsMatchAsSubquery = previousFtsMatchAsSubquery;

            if (result is not SQLiteExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
            }

            AllPredicate = sqlExpression;
        }
        else
        {
            CheckWhereArgument(node);
        }

        return node;
    }

    private void CheckWhereArgument(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2)
        {
            Expression stripped = ExpressionHelpers.StripQuotes(node.Arguments[1]);
            if (stripped is LambdaExpression lambda)
            {
                ThrowIfSetOperations(node.Method.Name);
                ThrowIfGroupJoinGroupPredicate(lambda.Body);
                Expression result = visitor.Visit(lambda.Body);

                if (result is not SQLiteExpression sqlExpression)
                {
                    throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
                }

                if (GroupBys.Count != 0)
                {
                    Havings.Add(sqlExpression);
                }
                else
                {
                    Wheres.Add(sqlExpression);
                }
            }
            else
            {
                CaptureDefaultValue(node.Arguments[1]);
            }
        }

        if (node.Arguments.Count == 3)
        {
            CaptureDefaultValue(node.Arguments[2]);
        }
    }

    private void ThrowIfGroupJoinGroupPredicate(Expression body)
    {
        List<Type> groupElementTypes = Joins.Where(f => f.GroupMemberPath != null).Select(f => f.EntityType).ToList();
        if (groupElementTypes.Count == 0)
        {
            return;
        }

        GroupJoinGroupUsageVisitor finder = new(groupElementTypes);
        finder.Visit(body);

        if (finder.Found)
        {
            throw new NotSupportedException(
                "GroupJoin (the LINQ 'into <name>' syntax) is only supported when flattened with " +
                "'from x in <name>' or 'from x in <name>.DefaultIfEmpty()'. Using the group in a filter " +
                "or ordering (for example 'g.Any()' or 'g.Count()') is not supported. " +
                "Rewrite the condition as a correlated subquery such as 'where db.Table<Book>().Any(b => b.AuthorId == a.Id)'.");
        }
    }

    private void CaptureDefaultValue(Expression expression)
    {
        ResolvedModel resolved = visitor.ResolveExpression(expression);
        if (!resolved.IsConstant)
        {
            throw new NotSupportedException("FirstOrDefault/SingleOrDefault default value must be a constant.");
        }

        DefaultValue = resolved.Constant;
        HasDefaultValue = true;
    }
}
