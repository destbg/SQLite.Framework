namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private Expression VisitWhere(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
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

        return result;
    }

    private MethodCallExpression VisitContains(MethodCallExpression node)
    {
        ThrowIfSetOperations(node.Method.Name);

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
            throw new Exception($"Unsupported expression type {node.Arguments[1].GetType().Name} in Contains.");
        }

        if (!IsInnerQuery)
        {
            SQLiteExpression columnExpr = (SQLiteExpression)visitor.TableColumns.Values.First();

            Wheres.Add(SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "", columnExpr, " = ", sqlExpression, "", sqlExpression.Parameters));

            IsAny = true;
        }

        return node;
    }

    private MethodCallExpression VisitScalar(MethodCallExpression node)
    {
        CheckWhereArgument(node);
        ThrowIfReverse(node.Method.Name);
        IsRowSelector = true;

        if (node.Method.Name is nameof(System.Linq.Queryable.Single) or nameof(System.Linq.Queryable.SingleOrDefault))
        {
            Take = Take.HasValue ? Math.Min(Take.Value, 2) : 2;
            ThrowOnMoreThanOne = true;
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
        IsAny = node.Method.Name == nameof(System.Linq.Queryable.Any);
        IsAll = node.Method.Name == nameof(System.Linq.Queryable.All);

        if (IsAll && node.Arguments.Count >= 2)
        {
            ThrowIfSetOperations(node.Method.Name);

            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            Expression result = visitor.Visit(lambda.Body);

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
            ThrowIfSetOperations(node.Method.Name);

            Expression stripped = ExpressionHelpers.StripQuotes(node.Arguments[1]);
            if (stripped is LambdaExpression lambda)
            {
                Expression result = visitor.Visit(lambda.Body);

                if (result is not SQLiteExpression sqlExpression)
                {
                    throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
                }

                Wheres.Add(sqlExpression);
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
