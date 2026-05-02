namespace SQLite.Framework.Internals.Visitors.Member;

internal static class QueryableMemberVisitor
{
    public static Expression HandleQueryableMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        SQLTranslator translator = visitor.CloneDeeper(visitor.Level + 1);
        SQLQuery query = translator.Translate(node);

        if (node.Method.Name is nameof(System.Linq.Queryable.Any) or nameof(System.Linq.Queryable.All))
        {
            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"EXISTS ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                query.Parameters.Count != 0
                    ? query.Parameters.ToArray()
                    : null
            );
        }

        if (node.Arguments.Count == 1 || node.Method.Name != nameof(System.Linq.Queryable.Contains))
        {
            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                query.Parameters.Count != 0
                    ? query.Parameters.ToArray()
                    : null
            );
        }

        SQLiteExpression innerSql = new(
            node.Method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"({Environment.NewLine}{query.Sql}{Environment.NewLine})",
            query.Parameters.Count != 0
                ? query.Parameters.ToArray()
                : null
        );

        List<ResolvedModel> arguments = node.Arguments
            .Skip(1)
            .Select(visitor.ResolveExpression)
            .ToList();

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters([innerSql, .. arguments.Select(f => f.SQLiteExpression!)]);

        return new SQLiteExpression(
            node.Method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"{arguments[0].Sql} IN ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
            parameters
        );
    }

    public static Expression HandleEnumerableMethod(SQLVisitor visitor, MethodCallExpression node, IEnumerable enumerable, List<ResolvedModel> arguments)
    {
        int firstItemArgIndex = node.Object == null ? 1 : 0;

        if (arguments.Skip(firstItemArgIndex).Any(f => f.Sql == null))
        {
            return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Object == null
            && TypeHelpers.IsSimple(node.Method.ReturnType, visitor.Database.Options)
            && arguments.Skip(firstItemArgIndex).All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, [
                enumerable,
                ..node.Arguments.Skip(1).Select(ExpressionHelpers.GetConstantValue)
            ]);
            string pName = $"@p{visitor.Counters.ParamIndex++}";

            return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, pName, result);
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
            {
                SQLiteParameter[] parameters = enumerable
                    .Cast<object>()
                    .Select(f => new SQLiteParameter
                    {
                        Name = $"@p{visitor.Counters.ParamIndex++}",
                        Value = f
                    })
                    .ToArray();

                int itemIndex = node.Object == null ? 1 : 0;
                ResolvedModel item = arguments[itemIndex];

                if (parameters.Length == 0)
                {
                    // For an empty list, `IN ()` is invalid SQL and should always return false.
                    // We use `0 = 1` to ensure the condition is never true.
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        "0 = 1",
                        item.Parameters
                    );
                }

                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"{item.Sql} IN ({string.Join(", ", parameters.Select(f => f.Name))})",
                    [.. item.Parameters ?? [], .. parameters]
                );
            }
        }

        return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
    }

    public static Expression HandleGroupingMethod(SQLVisitor visitor, MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(Enumerable.LongCount):
            case nameof(Enumerable.Count):
                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    "COUNT(*)",
                    []
                );
        }

        SQLiteExpression? sqlExpression = null;

        // We have a selection like `g.Sum(f => f.Price)`, where `g` is a grouping.
        if (node.Arguments.Count == 2)
        {
            (string path, ParameterExpression pe) = ExpressionHelpers.ResolveParameterPath(node.Arguments[0]);

            Dictionary<string, Expression> newTableColumns = [];

            foreach (KeyValuePair<string, Expression> kvp in visitor.MethodArguments[pe])
            {
                if (kvp.Key.StartsWith(path))
                {
                    // +1 for the dot between the path and the key
                    int length = path.Length + nameof(IGrouping<,>.Key).Length + 1;
                    string[] split = kvp.Key[Math.Min(length, kvp.Key.Length)..]
                        .Split('.', StringSplitOptions.RemoveEmptyEntries);

                    string newKey = string.Join('.', split);
                    newTableColumns[newKey] = kvp.Value;
                }
            }

            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
            visitor.MethodArguments[lambda.Parameters[0]] = newTableColumns;
        }
        else
        {
            // If the path is empty, we are dealing with a grouping without a key, like `g.Sum()`.
            // We need to resolve the grouping key to the correct column.
            Expression expression = visitor.ResolveMember(node.Arguments[0]);

            if (expression is not SQLiteExpression expr)
            {
                throw new NotSupportedException("Grouping key could not be resolved.");
            }

            sqlExpression = expr;
        }

        return node.Method.Name switch
        {
            nameof(Enumerable.Sum) => AggregateExpression(visitor, node, "SUM", sqlExpression),
            nameof(Enumerable.Average) => AggregateExpression(visitor, node, "AVG", sqlExpression),
            nameof(Enumerable.Min) => AggregateExpression(visitor, node, "MIN", sqlExpression),
            nameof(Enumerable.Max) => AggregateExpression(visitor, node, "MAX", sqlExpression),
            _ => throw new NotSupportedException($"Grouping aggregate {node.Method.Name} is not translatable to SQL.")
        };
    }

    private static SQLiteExpression AggregateExpression(SQLVisitor visitor, MethodCallExpression node, string aggregateFunction, SQLiteExpression? sqlExpression)
    {
        if (node.Arguments.Count == 1)
        {
            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"{aggregateFunction}({sqlExpression!.Sql})",
                sqlExpression.Parameters
            );
        }

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(node.Arguments[1]);
        Expression resolvedExpression = visitor.Visit(lambda.Body);
        if (resolvedExpression is not SQLiteExpression sql)
        {
            throw new NotSupportedException("Sum could not resolve the expression.");
        }

        return new SQLiteExpression(
            node.Method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"{aggregateFunction}({sql.Sql})",
            sql.Parameters
        );
    }

    internal static bool CheckConstantMethod<T>(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, [MaybeNullWhen(false)] out Expression expression)
    {
        if (arguments.Any(f => f.Sql == null))
        {
            expression = Expression.Call(node.Method, arguments.Select(f => f.Expression));
            return true;
        }

        Type type = typeof(T);

        if (node.Method.ReturnType.IsAssignableTo(type) && arguments.All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, arguments.Select(f => f.Constant).ToArray());

            string pName = $"@p{visitor.Counters.ParamIndex++}";
            expression = new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, pName, result);
            return true;
        }

        expression = null;
        return false;
    }
}
