using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Visitors;

internal class QueryableMethodVisitor
{
    private readonly SQLiteDatabase database;
    private readonly SelectVisitor selectVisitor;
    private readonly SQLVisitor visitor;

    public QueryableMethodVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        Selects = [];
        selectVisitor = new SelectVisitor(Selects);
    }

    public List<JoinInfo> Joins { get; } = [];
    public List<SQLExpression> Wheres { get; } = [];
    public List<SQLExpression> OrderBys { get; } = [];
    public List<(SQLExpression Sql, bool All)> Unions { get; } = [];
    public List<SQLExpression> Selects { get; }

    public int? Take { get; set; }
    public int? Skip { get; set; }
    public bool IsAny { get; set; }
    public bool IsAll { get; set; }
    public bool IsDistinct { get; set; }
    public bool ThrowOnEmpty { get; set; }
    public bool ThrowOnMoreThanOne { get; set; }

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
            nameof(Queryable.Count) => VisitGroupFunction(node, "COUNT"),
            nameof(Queryable.LongCount) => VisitGroupFunction(node, "COUNT"),
            nameof(Queryable.Sum) => VisitGroupFunction(node, "SUM"),
            nameof(Queryable.Max) => VisitGroupFunction(node, "MAX"),
            nameof(Queryable.Min) => VisitGroupFunction(node, "MIN"),
            nameof(Queryable.Average) => VisitGroupFunction(node, "AVG"),
            nameof(Queryable.Distinct) => VisitDistinct(node),
            nameof(Queryable.Concat) => VisitUnion(node),
            nameof(Queryable.Union) => VisitUnion(node),
            nameof(Queryable.Contains) => VisitContains(node),
            nameof(Queryable.GroupBy) => VisitGroupBy(node),
            _ => throw new NotSupportedException($"Unsupported method: {node.Method}")
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    private Expression VisitSelect(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        visitor.TableColumns = ResolveResultAlias(lambda, lambda.Body);

        Selects.Clear();

        if (visitor.TableColumns.All(f => f.Value is SQLExpression))
        {
            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                SQLExpression sqlExpression = (SQLExpression)tableColumn.Value;

                Selects.Add(new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    sqlExpression.Sql,
                    sqlExpression.Parameters
                )
                {
                    IdentifierText = tableColumn.Key
                });
            }

            return node;
        }
        else if (lambda.Body is ParameterExpression)
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
        else
        {
            Expression selectExpression = visitor.Visit(lambda.Body);
            Expression expression = selectVisitor.Visit(selectExpression);

            return expression;
        }
    }

    private Expression VisitWhere(MethodCallExpression node)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        Expression result = visitor.Visit(lambda.Body);

        if (result is not SQLExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported WHERE expression {lambda.Body}");
        }

        Wheres.Add(sqlExpression);

        return result;
    }

    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    private Expression VisitJoin(MethodCallExpression node)
    {
        Dictionary<string, Expression> newTableColumns;
        Type entityType;
        SQLExpression sql;

        if (CommonHelpers.IsConstant(node.Arguments[1]))
        {
            object? innerValue = CommonHelpers.GetConstantValue(node.Arguments[1]);

            if (innerValue is SQLiteTable table)
            {
                entityType = table.ElementType;
                char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
                string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

                TableMapping tableMapping = database.TableMapping(entityType);
                newTableColumns = tableMapping.Columns
                    .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
                sql = new SQLExpression(node.Arguments[1].Type, -1, $"\"{table.Table.TableName}\" AS {alias}");
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
                    node.Arguments[1].Type,
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
        else if (node.Arguments[1].Type.IsGenericType && node.Arguments[1].Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
            SQLQuery query = innerVisitor.Translate(node.Arguments[1]);

            entityType = node.Arguments[1].Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
            string alias = $"{aliasChar}{visitor.TableIndex[aliasChar]++}";

            newTableColumns = entityType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, visitor.IdentifierIndex++, $"{alias}.{f.Name}"));
            sql = new SQLExpression(
                node.Arguments[1].Type,
                -1,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine}) AS {alias}",
                query.Parameters.Count != 0 ? query.Parameters.ToArray() : null
            );
        }
        else
        {
            throw new NotSupportedException($"The type {node.Arguments[1].GetType().Name} is not supported in join.");
        }

        LambdaExpression outerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);
        LambdaExpression innerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[3]);
        LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[4]);

        visitor.MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
        visitor.MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

        visitor.TableColumns = ResolveResultAlias(resultSelector, resultSelector.Body);

        visitor.MethodArguments[innerKey.Parameters[0]] = newTableColumns;

        SQLExpression outerAlias = (SQLExpression)visitor.Visit(outerKey.Body);
        SQLExpression innerAlias = (SQLExpression)visitor.Visit(innerKey.Body);

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(outerAlias, innerAlias);

        Joins.Add(new JoinInfo
        {
            EntityType = entityType,
            JoinType = node.Method.Name == nameof(Queryable.GroupJoin) ? "LEFT JOIN" : "JOIN",
            Sql = sql,
            OnClause = new SQLExpression(typeof(bool), -1, $"{outerAlias} = {innerAlias}", parameters),
            IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
        });

        return sql;
    }

    private Expression VisitSelectMany(MethodCallExpression node)
    {
        LambdaExpression selector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);

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

        visitor.TableColumns = ResolveResultAlias(resultSelector, resultSelector.Body);

        return node;
    }

    private Expression VisitTake(MethodCallExpression node)
    {
        Take = (int)CommonHelpers.GetConstantValue(node.Arguments[1])!;
        return node;
    }

    private Expression VisitSkip(MethodCallExpression node)
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

    private Expression VisitScalar(MethodCallExpression node)
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

    private Expression VisitBoolean(MethodCallExpression node)
    {
        CheckWhereArgument(node);
        IsAny = node.Method.Name == nameof(Queryable.Any);
        IsAll = node.Method.Name == nameof(Queryable.All);

        return node;
    }

    private Expression VisitGroupFunction(MethodCallExpression node, string function)
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

            select = new SQLExpression(node.Arguments[1].Type, visitor.IdentifierIndex++, $"{function}({sqlExpression.Sql})", sqlExpression.Parameters);
        }
        else if (Selects.Count == 1)
        {
            select = new SQLExpression(Selects[0].Type, visitor.IdentifierIndex++, $"{function}({Selects[0].Sql})", Selects[0].Parameters);
        }
        else
        {
            throw new NotSupportedException($"A problem occured while compiling the {function} expression.");
        }

        Selects.Clear();
        Selects.Add(select);

        return select;
    }

    private Expression VisitDistinct(MethodCallExpression node)
    {
        IsDistinct = true;
        return node;
    }

    private Expression VisitUnion(MethodCallExpression node)
    {
        SQLTranslator sqlTranslator = visitor.CloneDeeper(visitor.Level);
        SQLQuery query = sqlTranslator.Translate(node.Arguments[0]);

        SQLExpression sqlExpression = new(
            node.Arguments[0].Type,
            visitor.IdentifierIndex++,
            query.Sql,
            query.Parameters.Count == 0 ? null : query.Parameters.ToArray()
        );

        Unions.Add((sqlExpression, node.Method.Name == nameof(Queryable.Concat)));

        return sqlExpression;
    }

    private Expression VisitContains(MethodCallExpression node)
    {
        if (visitor.TableColumns.Count != 1)
        {
            throw new NotSupportedException("Contains is only supported for a single column.");
        }

        object? value = CommonHelpers.GetConstantValue(node.Arguments[1]);

        if (value != null && !CommonHelpers.IsSimple(value.GetType()))
        {
            throw new NotSupportedException("Contains is only supported for a single column.");
        }

        if (!IsInnerQuery)
        {
            string columnName = ((SQLExpression)visitor.TableColumns.Values.First()).Sql;
            string pName = $"@p{visitor.ParamIndex.Index++}";
            SQLiteParameter parameter = new()
            {
                Name = pName,
                Value = value,
            };

            Wheres.Add(new SQLExpression(typeof(bool), visitor.IdentifierIndex++, $"{columnName} = {pName}", [parameter]));

            IsAny = true;
        }

        return node;
    }

    private Expression VisitGroupBy(MethodCallExpression node)
    {
        // LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        // visitor.TableColumns = ResolveResultAlias(lambda, lambda.Body);

        // TODO: Add GroupBy support
        throw new NotSupportedException("GroupBy is not supported.");
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

    private Dictionary<string, Expression> ResolveResultAlias(LambdaExpression lambdaExpression, Expression body, string? prefix = null)
    {
        Dictionary<string, Expression> result = [];

        if (body is NewExpression newExpression)
        {
            if (newExpression.Arguments.Count > 0)
            {
                foreach (Expression argument in newExpression.Arguments)
                {
                    if (argument is ParameterExpression parameterExpression)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{parameterExpression.Name}";
                        Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                        foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                        {
                            result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                        }
                    }
                    else if (argument is MemberExpression memberExpression)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}";
                        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(memberExpression);

                        Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                        if (CommonHelpers.IsSimple(memberExpression.Type))
                        {
                            result.Add(alias, parameterTableColumns[path]);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                            {
                                if (tableColumn.Key.StartsWith(path))
                                {
                                    result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                                }
                            }
                        }
                    }
                    else if (argument is ParameterExpression parameterExpr)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{parameterExpr.Name}";
                        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(parameterExpr);

                        Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                        if (CommonHelpers.IsSimple(parameterExpr.Type))
                        {
                            result.Add(alias, parameterTableColumns[path]);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                            {
                                if (tableColumn.Key.StartsWith(path))
                                {
                                    result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported member expression {argument}");
                    }
                }
            }
            else if (newExpression.Members == null)
            {
                throw new NotSupportedException("Cannot translate expression");
            }
            else
            {
                foreach (MemberInfo memberInfo in newExpression.Members)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberInfo.Name}";
                    Type propertyType = memberInfo is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)memberInfo).FieldType;

                    ParameterExpression expression = lambdaExpression.Parameters
                        .First(f => (f.Name == memberInfo.Name && f.Type == propertyType) || f.Type == propertyType);

                    (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(expression);

                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        if (tableColumn.Key.StartsWith(path))
                        {
                            result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                        }
                    }
                }
            }
        }
        else if (body is MemberInitExpression memberInitExpression)
        {
            foreach (MemberAssignment memberAssignment in memberInitExpression.Bindings.Cast<MemberAssignment>())
            {
                if (memberAssignment.Expression is MemberInitExpression or NewExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    Dictionary<string, Expression> innerResult = ResolveResultAlias(lambdaExpression, memberAssignment.Expression, alias);

                    foreach (KeyValuePair<string, Expression> tableColumn in innerResult)
                    {
                        result.Add(tableColumn.Key, tableColumn.Value);
                    }
                }
                else if (memberAssignment.Expression is ParameterExpression parameterExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                    }
                }
                else if (memberAssignment.Expression is MemberExpression or ParameterExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(memberAssignment.Expression);

                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                    if (CommonHelpers.IsSimple(memberAssignment.Expression.Type))
                    {
                        result.Add(alias, parameterTableColumns[path]);
                    }
                    else
                    {
                        foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                        {
                            if (tableColumn.Key.StartsWith(path))
                            {
                                result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                            }
                        }
                    }
                }
                else
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    SQLVisitor innerVisitor = new(database, visitor.ParamIndex, visitor.TableIndex, visitor.Level + 1)
                    {
                        MethodArguments = visitor.MethodArguments
                    };
                    Expression expression = innerVisitor.Visit(memberAssignment.Expression);
                    result.Add(alias, expression);
                }
            }
        }
        else if (body is MemberExpression memberExpression)
        {
            if (CommonHelpers.IsSimple(memberExpression.Type))
            {
                (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(body);

                Expression columnMapping = visitor.TableColumns[path];
                result.Add(memberExpression.Member.Name, columnMapping);
            }
            else
            {
                (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(body);

                foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
                {
                    if (tableColumn.Key.StartsWith(path))
                    {
                        string newPath = tableColumn.Key[(path.Length + 1)..];
                        result.Add(newPath, tableColumn.Value);
                    }
                }
            }
        }
        else if (body is ParameterExpression pe)
        {
            Dictionary<string, Expression> tableColumns = visitor.MethodArguments[pe];

            foreach (KeyValuePair<string, Expression> tableColumn in tableColumns)
            {
                result.Add(tableColumn.Key, tableColumn.Value);
            }
        }
        else
        {
            SQLVisitor innerVisitor = new(database, visitor.ParamIndex, visitor.TableIndex, visitor.Level + 1)
            {
                MethodArguments = visitor.MethodArguments
            };
            Expression sql = innerVisitor.Visit(body);

            if (sql is not SQLExpression sqlExpression)
            {
                throw new NotSupportedException($"Unsupported expression {body}");
            }

            result.Add(string.Empty, sqlExpression);
        }

        return result;
    }

    // private void BuildSelect(Expression expr, string? prefix)
    // {
    //     if (expr is MemberInitExpression mi)
    //     {
    //         // new TDto { Prop = ..., Prop2 = ... }
    //         foreach (MemberAssignment bind in mi.Bindings.Cast<MemberAssignment>())
    //         {
    //             // 1) Nested DTO: e.g. Author = new AuthorDTO { Id = ..., Name = ... }
    //             if (bind.Expression is MemberInitExpression nested && bind.Member is PropertyInfo pi)
    //             {
    //                 BuildSelect(nested, pi.Name);
    //             }
    //             else
    //             {
    //                 string path = $"{(prefix != null ? $"{prefix}." : string.Empty)}{bind.Member.Name}";
    //
    //                 ColumnMapping mapping = TableColumns[path];
    //                 Selects.Add($"{CommonHelpers.BracketIfNeeded(mapping.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{bind.Member.Name}\"");
    //             }
    //         }
    //     }
    //     else if (expr is NewExpression nex)
    //     {
    //         // new { Prop = ..., Prop2 = ... }
    //         foreach (Expression bind in nex.Arguments)
    //         {
    //             // 1) Nested DTO: e.g. Author = new AuthorDTO { Id = ..., Name = ... }
    //             if (bind is MemberExpression memberExpression)
    //             {
    //                 string path = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}";
    //
    //                 ColumnMapping mapping = TableColumns[path];
    //                 Selects.Add($"{CommonHelpers.BracketIfNeeded(mapping.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}\"");
    //             }
    //             else
    //             {
    //                 throw new NotSupportedException($"Unsupported member expression {nex}");
    //             }
    //         }
    //     }
    //     else if (expr is MemberExpression me)
    //     {
    //         if (CommonHelpers.IsSimple(me.Type))
    //         {
    //             (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(me);
    //
    //             if (TableColumns.TryGetValue(path, out ColumnMapping? tableColumn))
    //             {
    //                 Selects.Add($"{CommonHelpers.BracketIfNeeded(tableColumn.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{me.Member.Name}\"");
    //             }
    //         }
    //         else
    //         {
    //             foreach (KeyValuePair<string, ColumnMapping> tableColumn in TableColumns)
    //             {
    //                 string sql = tableColumn.Value.Sql;
    //                 Selects.Add($"{CommonHelpers.BracketIfNeeded(sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{tableColumn.Value.PropertyName}\"");
    //             }
    //         }
    //     }
    //     else if (TableColumns.TryGetValue(string.Empty, out ColumnMapping? tableColumn))
    //     {
    //         Selects.Add(CommonHelpers.BracketIfNeeded(tableColumn.Sql));
    //     }
    //     else if (expr is ParameterExpression)
    //     {
    //         foreach (KeyValuePair<string, ColumnMapping> tableColumn2 in TableColumns)
    //         {
    //             string sql = tableColumn2.Value.Sql;
    //             Selects.Add($"{CommonHelpers.BracketIfNeeded(sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{tableColumn2.Key}\"");
    //         }
    //     }
    //     else
    //     {
    //         throw new NotSupportedException("Only simple .Select(new DTO { … }) or .Select(f => f.Id) is supported");
    //     }
    // }
}