using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Goes through the LINQ methods and gets the different SQL query parts from them.
/// </summary>
internal class QueryableMethodVisitor
{
    private readonly AliasVisitor aliasVisitor;
    private readonly SQLiteDatabase database;
    private readonly SelectVisitor selectVisitor;
    private readonly SQLVisitor visitor;

    public QueryableMethodVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        Selects = [];
        selectVisitor = new SelectVisitor(Selects);
        aliasVisitor = new AliasVisitor(database, visitor);
    }

    public List<JoinInfo> Joins { get; } = [];
    public List<SQLExpression> Wheres { get; } = [];
    public List<SQLExpression> OrderBys { get; } = [];
    public List<SQLExpression> GroupBys { get; } = [];
    public List<SQLExpression> Havings { get; } = [];
    public List<(SQLExpression Sql, string Type)> SetOperations { get; } = [];
    public List<SQLExpression> Selects { get; }

    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsAny { get; private set; }
    public bool IsAll { get; private set; }
    public bool Reverse { get; private set; }
    public bool IsDistinct { get; private set; }
    public bool ThrowOnEmpty { get; private set; }
    public bool ThrowOnMoreThanOne { get; private set; }

    public bool IsInnerQuery { get; set; }

    public Expression Visit(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(Queryable.Select) => VisitSelect(node),
            nameof(Queryable.Where) => VisitWhere(node),
            nameof(Queryable.Join) => VisitJoin(node),
            nameof(Queryable.GroupJoin) => VisitJoin(node),
            nameof(Queryable.SelectMany) => VisitSelectMany(node),
            nameof(Queryable.Take) => VisitTake(node),
            nameof(Queryable.Skip) => VisitSkip(node),
            nameof(Queryable.OrderBy) => VisitOrder(node),
            nameof(Queryable.OrderByDescending) => VisitOrder(node),
            nameof(Queryable.ThenBy) => VisitOrder(node),
            nameof(Queryable.ThenByDescending) => VisitOrder(node),
            nameof(Queryable.First) => VisitScalar(node),
            nameof(Queryable.FirstOrDefault) => VisitScalar(node),
            nameof(Queryable.Single) => VisitScalar(node),
            nameof(Queryable.SingleOrDefault) => VisitScalar(node),
            nameof(Queryable.Any) => VisitBoolean(node),
            nameof(Queryable.All) => VisitBoolean(node),
            nameof(Queryable.Count)
                or nameof(Queryable.LongCount) => VisitGroupFunction(node, "COUNT"),
            nameof(Queryable.Sum) => VisitGroupFunction(node, "SUM"),
            nameof(Queryable.Max) => VisitGroupFunction(node, "MAX"),
            nameof(Queryable.Min) => VisitGroupFunction(node, "MIN"),
            nameof(Queryable.Average) => VisitGroupFunction(node, "AVG"),
            nameof(Queryable.Distinct) => VisitDistinct(node),
            nameof(Queryable.Concat)
                or nameof(Queryable.Union)
                or nameof(Queryable.Intersect)
                or nameof(Queryable.Except) => VisitSetOperation(node),
            nameof(Queryable.Contains) => VisitContains(node),
            nameof(Queryable.GroupBy) => VisitGroupBy(node),
            nameof(Queryable.Reverse) => VisitReverse(node),
            nameof(Queryable.Cast) => node,
            nameof(SQLiteDatabase.FromSql) => VisitFromSql(node),
            _ => throw new NotSupportedException($"Unsupported method: {node.Method}")
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    private Expression VisitSelect(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        visitor.TableColumns = aliasVisitor.ResolveResultAlias(lambda);

        Selects.Clear();

        if (visitor.TableColumns.All(f => f.Value is SQLExpression))
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                SQLExpression sqlExpression = (SQLExpression)tableColumn.Value;

                SQLExpression newSqlExpression = new(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    sqlExpression.Sql,
                    sqlExpression.Parameters
                );

                if (!string.IsNullOrEmpty(tableColumn.Key))
                {
                    newSqlExpression.IdentifierText = tableColumn.Key;
                }

                Selects.Add(newSqlExpression);
            }

            return node;
        }

        if (lambda.Body is ParameterExpression)
        {
            return Expression.MemberInit(
                Expression.New(lambda.Body.Type),
                visitor.TableColumns.Select(tableColumn =>
                    Expression.Bind(
                        lambda.Body.Type.GetProperty(tableColumn.Key)!,
                        selectVisitor.Visit(visitor.Visit(tableColumn.Value))
                    )
                )
            );
        }

        Expression selectExpression = visitor.Visit(lambda.Body);
        Expression expression = selectVisitor.Visit(selectExpression);

        return expression;
    }

    private Expression VisitWhere(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        Expression result = visitor.Visit(lambda.Body);

        if (result is not SQLExpression sqlExpression)
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

    private SQLExpression VisitJoin(MethodCallExpression node)
    {
        (Dictionary<string, Expression> newTableColumns, Type entityType, SQLExpression sql) = ResolveTable(node.Arguments[1]);

        LambdaExpression outerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);
        LambdaExpression innerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[3]);
        LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[4]);

        visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
        visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

        visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

        visitor.MethodArguments[innerKey.Parameters[0]] = newTableColumns;

        if (outerKey.Body is NewExpression outerNewExpression)
        {
            NewExpression innerNewExpression = (NewExpression)innerKey.Body;

            if (outerNewExpression.Arguments.Count != innerNewExpression.Arguments.Count)
            {
                throw new NotSupportedException($"The outer and inner key expressions must have the same number of arguments. Outer: {outerNewExpression.Arguments.Count}, Inner: {innerNewExpression.Arguments.Count}");
            }

            List<SQLExpression> sqlExpressions = [];

            for (int i = 0; i < innerNewExpression.Arguments.Count; i++)
            {
                Expression innerArgument = innerNewExpression.Arguments[i];
                Expression outerArgument = outerNewExpression.Arguments[i];

                SQLExpression outerAlias = (SQLExpression)visitor.Visit(innerArgument);
                SQLExpression innerAlias = (SQLExpression)visitor.Visit(outerArgument);

                SQLiteParameter[]? combinedParameters = CommonHelpers.CombineParameters(outerAlias, innerAlias);

                string comparison = $"{outerAlias.Sql} = {innerAlias.Sql}";
                sqlExpressions.Add(new SQLExpression(typeof(bool), -1, comparison, combinedParameters));
            }

            string onClause = string.Join(" AND ", sqlExpressions.Select(f => f.Sql));
            SQLiteParameter[]? sqlParameters = CommonHelpers.CombineParameters(sqlExpressions.ToArray());

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = node.Method.Name == nameof(Queryable.GroupJoin) ? "LEFT JOIN" : "JOIN",
                Sql = sql,
                OnClause = new SQLExpression(typeof(bool), -1, onClause, sqlParameters),
                IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
            });
        }
        else
        {
            SQLExpression outerAlias = (SQLExpression)visitor.Visit(outerKey.Body);
            SQLExpression innerAlias = (SQLExpression)visitor.Visit(innerKey.Body);

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(outerAlias, innerAlias);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = node.Method.Name == nameof(Queryable.GroupJoin) ? "LEFT JOIN" : "JOIN",
                Sql = sql,
                OnClause = new SQLExpression(typeof(bool), -1, $"{outerAlias.Sql} = {innerAlias.Sql}", parameters),
                IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
            });
        }

        return sql;
    }

    private MethodCallExpression VisitSelectMany(MethodCallExpression node)
    {
        LambdaExpression selector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);

        if (selector.Body is MethodCallExpression { Method.Name: nameof(Enumerable.DefaultIfEmpty) })
        {
            // TODO: This gets the last group join of a given type, but if we have two group joins of the same type it may not work.
            if (Joins.Count > 0)
            {
                Type type = selector.Body.Type.GetGenericArguments()[^1];
                JoinInfo join = Joins.First(f => f.EntityType == type && f.IsGroupJoin);
                join.JoinType = "LEFT JOIN";
                join.IsGroupJoin = false;
            }

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;

            if (selector.Body is MethodCallExpression methodCallExpression && methodCallExpression.Arguments[0] is MemberExpression memberExpression)
            {
                Dictionary<string, Expression> result = [];

                foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
                {
                    if (tableColumn.Key.StartsWith(memberExpression.Member.Name))
                    {
                        string path = tableColumn.Key[(memberExpression.Member.Name.Length + 1)..];
                        result.Add(path, tableColumn.Value);
                    }
                }

                visitor.MethodArguments[resultSelector.Parameters[1]] = result;
            }

            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);
        }
        else
        {
            (Dictionary<string, Expression> newTableColumns, Type entityType, SQLExpression sql) = ResolveTable(selector.Body);

            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

            Joins.Add(new JoinInfo
            {
                EntityType = entityType,
                JoinType = "CROSS JOIN",
                Sql = sql,
                OnClause = null,
                IsGroupJoin = false
            });
        }

        return node;
    }

    private MethodCallExpression VisitTake(MethodCallExpression node)
    {
        Take = (int)CommonHelpers.GetConstantValue(node.Arguments[1])!;
        return node;
    }

    private MethodCallExpression VisitSkip(MethodCallExpression node)
    {
        Skip = (int)CommonHelpers.GetConstantValue(node.Arguments[1])!;
        return node;
    }

    private Expression VisitOrder(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        Expression orderBy = visitor.Visit(lambda.Body);

        if (orderBy is not SQLExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported ORDER BY expression {lambda.Body}");
        }

        if (node.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderDescending))
        {
            OrderBys.Clear();
        }

        string order = node.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.ThenBy)
            ? "ASC"
            : "DESC";

        OrderBys.Add(new SQLExpression(node.Arguments[1].Type, visitor.IdentifierIndex++, $"{sqlExpression.Sql} {order}", sqlExpression.Parameters));
        return orderBy;
    }

    private MethodCallExpression VisitScalar(MethodCallExpression node)
    {
        CheckWhereArgument(node);

        if (node.Method.Name is nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault))
        {
            Take = 2;
            ThrowOnMoreThanOne = true;
        }
        else
        {
            Take = 1;
        }

        if (node.Method.Name is nameof(Queryable.First) or nameof(Queryable.Single))
        {
            ThrowOnEmpty = true;
        }

        return node;
    }

    private MethodCallExpression VisitBoolean(MethodCallExpression node)
    {
        CheckWhereArgument(node);
        IsAny = node.Method.Name == nameof(Queryable.Any);
        IsAll = node.Method.Name == nameof(Queryable.All);

        return node;
    }

    private SQLExpression VisitGroupFunction(MethodCallExpression node, string function)
    {
        SQLExpression select;
        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
            Expression expression = visitor.Visit(lambda.Body);

            if (expression is not SQLExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported {function} expression {lambda.Body}");
            }

            if (function == "COUNT")
            {
                Wheres.Add(sqlExpression);
                select = new SQLExpression(node.Arguments[0].Type, visitor.IdentifierIndex++, $"{function}(*)");
            }
            else
            {
                select = new SQLExpression(node.Arguments[1].Type, visitor.IdentifierIndex++, $"{function}({sqlExpression.Sql})", sqlExpression.Parameters);
            }
        }
        else if (function == "COUNT")
        {
            select = new SQLExpression(node.Arguments[0].Type, visitor.IdentifierIndex++, $"{function}(*)");
        }
        else if (Selects.Count == 1)
        {
            select = new SQLExpression(Selects[0].Type, visitor.IdentifierIndex++, $"{function}({Selects[0].Sql})", Selects[0].Parameters);
        }
        else
        {
            select = new SQLExpression(
                node.Arguments[0].Type,
                visitor.IdentifierIndex++,
                $"{function}(*)"
            );
        }

        Selects.Clear();
        Selects.Add(select);

        return select;
    }

    private MethodCallExpression VisitDistinct(MethodCallExpression node)
    {
        IsDistinct = true;
        return node;
    }

    private SQLExpression VisitSetOperation(MethodCallExpression node)
    {
        SQLTranslator sqlTranslator = visitor.CloneDeeper(visitor.Level);
        SQLQuery query = sqlTranslator.Translate(node.Arguments[1]);

        SQLExpression sqlExpression = new(
            node.Arguments[1].Type,
            visitor.IdentifierIndex++,
            query.Sql,
            query.Parameters.Count == 0 ? null : query.Parameters.ToArray()
        );

        SetOperations.Add((sqlExpression, node.Method.Name switch
        {
            nameof(Queryable.Concat) => "UNION ALL",
            nameof(Queryable.Union) => "UNION",
            nameof(Queryable.Intersect) => "INTERSECT",
            nameof(Queryable.Except) => "EXCEPT",
            _ => throw new Exception("Unsupported union or intersect")
        }));

        return sqlExpression;
    }

    private MethodCallExpression VisitContains(MethodCallExpression node)
    {
        if (visitor.TableColumns.Count != 1)
        {
            throw new NotSupportedException("Contains is only supported for a single column.");
        }

        ResolvedModel resolved = visitor.ResolveExpression(node.Arguments[1]);
        SQLExpression sqlExpression;

        if (resolved.IsConstant)
        {
            if (resolved.Constant != null && !CommonHelpers.IsSimple(resolved.Constant.GetType()))
            {
                throw new NotSupportedException("Contains is only supported for a single column.");
            }

            sqlExpression = resolved.SQLExpression!;
        }
        else if (resolved.SQLExpression != null)
        {
            sqlExpression = resolved.SQLExpression;
        }
        else
        {
            throw new Exception($"Unsupported expression type {node.Arguments[1].GetType().Name} in Contains.");
        }

        if (!IsInnerQuery)
        {
            string columnName = ((SQLExpression)visitor.TableColumns.Values.First()).Sql;

            Wheres.Add(new SQLExpression(typeof(bool), visitor.IdentifierIndex++, $"{columnName} = {sqlExpression.Sql}", sqlExpression.Parameters));

            IsAny = true;
        }

        return node;
    }

    private MethodCallExpression VisitGroupBy(MethodCallExpression node)
    {
        if (GroupBys.Count != 0)
        {
            throw new NotSupportedException("GroupBy is only supported once in a query.");
        }

        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);

        SelectVisitor groupByVisitor = new(GroupBys);
        Expression groupByExpression = visitor.Visit(lambda.Body);
        groupByVisitor.Visit(groupByExpression);

        if (GroupBys.Count == 0)
        {
            throw new NotSupportedException("There was a problem when compiling the GroupBy expression.");
        }

        bool isMember = false;

        if (node.Arguments.Count == 3)
        {
            LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);
            visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
            visitor.TableColumns = aliasVisitor.ResolveResultAlias(resultSelector);

            if (resultSelector.Body is MemberExpression && CommonHelpers.IsSimple(resultSelector.Body.Type))
            {
                isMember = true;
            }
        }

        Dictionary<string, Expression> newTableColumns = [];

        if (!isMember)
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                string[] split = tableColumn.Key.Split('.');
                string key = string.Join('.', [nameof(IGrouping<,>.Key), .. split]);

                newTableColumns[key] = tableColumn.Value;
            }
        }
        else
        {
            newTableColumns[string.Empty] = visitor.TableColumns.Single().Value;
        }

        newTableColumns[nameof(IGrouping<,>.Key)] = GroupBys[0];

        visitor.TableColumns = newTableColumns;

        return node;
    }

    private MethodCallExpression VisitReverse(MethodCallExpression node)
    {
        Reverse = !Reverse;
        return node;
    }

    private MethodCallExpression VisitFromSql(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        string sql = (string)CommonHelpers.GetConstantValue(node.Arguments[0])!;
        IEnumerable<object> arguments = (IEnumerable<object>)CommonHelpers.GetConstantValue(node.Arguments[1])!;
        SQLiteParameter[] parameters = arguments.Select(a => (SQLiteParameter)a).ToArray();

        visitor.AssignTable(genericType, new SQLExpression(genericType, -1, sql, parameters.Length == 0 ? null : parameters));
        return node;
    }

    private void CheckWhereArgument(MethodCallExpression node)
    {
        if (node.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
            Expression result = visitor.Visit(lambda.Body);

            if (result is not SQLExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
            }

            Wheres.Add(sqlExpression);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type is an entity.")]
    private (Dictionary<string, Expression> TableColmns, Type Type, SQLExpression Sql) ResolveTable(Expression body)
    {
        Dictionary<string, Expression> newTableColumns;
        Type entityType;
        SQLExpression sql;

        if (body is MethodCallExpression methodCall && methodCall.Method.ReturnType.IsAssignableTo(typeof(BaseSQLiteTable)))
        {
            object? obj = methodCall.Object != null
                ? CommonHelpers.GetConstantValue(methodCall.Object!)
                : null;
            BaseSQLiteTable resultTable = (BaseSQLiteTable)methodCall.Method.Invoke(obj, null)!;

            entityType = resultTable.ElementType;
            char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
            string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

            TableMapping tableMapping = database.TableMapping(entityType);
            newTableColumns = tableMapping.Columns
                .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
            sql = new SQLExpression(body.Type, -1, $"\"{tableMapping.TableName}\" AS {alias}");
        }
        else if (CommonHelpers.IsConstant(body))
        {
            object? innerValue = CommonHelpers.GetConstantValue(body);

            if (innerValue is SQLiteTable table)
            {
                entityType = table.ElementType;
                char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
                string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

                TableMapping tableMapping = database.TableMapping(entityType);
                newTableColumns = tableMapping.Columns
                    .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
                sql = new SQLExpression(body.Type, -1, $"\"{table.Table.TableName}\" AS {alias}");
            }
            else if (innerValue is IQueryable queryable)
            {
                SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
                SQLQuery query = innerVisitor.Translate(queryable.Expression);

                entityType = queryable.ElementType;
                char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
                string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

                newTableColumns = entityType.GetProperties()
                    .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
                sql = new SQLExpression(
                    body.Type,
                    -1,
                    $"({Environment.NewLine}{query.Sql}{Environment.NewLine}) AS {alias}",
                    query.Parameters.Count != 0 ? query.Parameters.ToArray() : null
                );
            }
            else
            {
                throw new NotSupportedException($"The type {innerValue?.GetType().Name} is not supported in join.");
            }
        }
        else if (body.Type.IsGenericType && body.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
            SQLQuery query = innerVisitor.Translate(body);

            entityType = body.Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

            newTableColumns = entityType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
            sql = new SQLExpression(
                body.Type,
                -1,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine}) AS {alias}",
                query.Parameters.Count != 0 ? query.Parameters.ToArray() : null
            );
        }
        else
        {
            throw new NotSupportedException($"The type {body.GetType().Name} is not supported in join.");
        }

        return (newTableColumns, entityType, sql);
    }
}