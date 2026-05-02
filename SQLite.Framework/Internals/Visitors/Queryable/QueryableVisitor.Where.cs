namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private Expression VisitWhere(MethodCallExpression node)
    {
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
            string columnName = ((SQLiteExpression)visitor.TableColumns.Values.First()).Sql;

            Wheres.Add(new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++, $"{columnName} = {sqlExpression.Sql}", sqlExpression.Parameters));

            IsAny = true;
        }

        return node;
    }

    private MethodCallExpression VisitScalar(MethodCallExpression node)
    {
        CheckWhereArgument(node);

        if (node.Method.Name is nameof(System.Linq.Queryable.Single) or nameof(System.Linq.Queryable.SingleOrDefault))
        {
            Take = 2;
            ThrowOnMoreThanOne = true;
        }
        else
        {
            Take = 1;
        }

        if (node.Method.Name is nameof(System.Linq.Queryable.First) or nameof(System.Linq.Queryable.Single))
        {
            ThrowOnEmpty = true;
        }

        return node;
    }

    private MethodCallExpression VisitBoolean(MethodCallExpression node)
    {
        CheckWhereArgument(node);
        IsAny = node.Method.Name == nameof(System.Linq.Queryable.Any);
        IsAll = node.Method.Name == nameof(System.Linq.Queryable.All);

        return node;
    }

    private void CheckWhereArgument(MethodCallExpression node)
    {
        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            Expression result = visitor.Visit(lambda.Body);

            if (result is not SQLiteExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
            }

            Wheres.Add(sqlExpression);
        }
    }
}
