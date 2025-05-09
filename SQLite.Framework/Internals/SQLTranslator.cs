using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals;

internal class SQLTranslator
{
    private readonly SQLiteDatabase database;
    private readonly List<SQLiteParameter> parameters;
    private readonly SQLVisitor visitor;
    private readonly int level;
    private readonly IndexWrapper paramIndex;
    private readonly IndexWrapper tableIndex;

    public SQLTranslator(SQLiteDatabase database)
    {
        this.database = database;
        parameters = new();
        paramIndex = new();
        tableIndex = new();
        visitor = new SQLVisitor(database, parameters, paramIndex, tableIndex, level);
    }

    public SQLTranslator(SQLiteDatabase database, List<SQLiteParameter> parameters, IndexWrapper paramIndex, IndexWrapper tableIndex, int level)
    {
        this.database = database;
        this.parameters = parameters;
        this.level = level;
        this.paramIndex = paramIndex;
        this.tableIndex = tableIndex;
        visitor = new SQLVisitor(database, parameters, paramIndex, tableIndex, level);
    }

    public Dictionary<ParameterExpression, Dictionary<string, ColumnMapping>> MethodArguments
    {
        get => visitor.MethodArguments;
        init => visitor.MethodArguments = value;
    }

    public SQLQuery Translate(Expression expression)
    {
        if (expression is MethodCallExpression mce)
        {
            TranslateMethodExpression(mce);
        }
        else
        {
            TranslateOtherExpression(expression);
        }

        if (visitor.From == null)
        {
            throw new InvalidOperationException("Could not identify FROM clause.");
        }

        string spacing = new(' ', level * 4);

        if (visitor.Joins.Any(f => f.IsGroupJoin))
        {
            throw new NotSupportedException("Group joins that are not turned into LEFT JOIN are not supported.");
        }

        bool useExists = visitor.IsAny || visitor.IsAll;

        string joinSql = string.Join(Environment.NewLine + spacing, visitor.Joins.Select(j =>
            $"{j.JoinType} {j.Sql} AS {j.Alias} ON {j.OnClause}"));

        string whereSql = visitor.Wheres.Count > 0
            ? "WHERE " + (visitor.IsAll
                ? $"NOT ({string.Join(" AND ", visitor.Wheres)})"
                : string.Join(" AND ", visitor.Wheres))
            : string.Empty;

        string distinct = visitor.IsDistinct ? " DISTINCT" : string.Empty;

        string selectSql = visitor.Selects.Count > 0 && !useExists
            ? string.Join($",{Environment.NewLine}       ", visitor.Selects)
            : "*";

        string orderBy = visitor.OrderBys.Count > 0 && !useExists
            ? "ORDER BY " + string.Join(", ", visitor.OrderBys)
            : string.Empty;

        string limit = visitor.Take != null ? $"LIMIT {visitor.Take}" : visitor.Skip != null ? "LIMIT -1" : string.Empty;
        string offset = visitor.Skip != null ? $"OFFSET {visitor.Skip}" : string.Empty;

        string sql = spacing + string.Join(Environment.NewLine + spacing, new[]
        {
            $"SELECT{distinct} {(useExists ? "1" : selectSql)}",
            $"FROM {visitor.From}",
            joinSql,
            whereSql,
            orderBy,
            limit,
            offset,
        }.Where(f => !string.IsNullOrEmpty(f)));

        if (visitor.Unions.Count > 0)
        {
            string unions = string.Join(Environment.NewLine + spacing, visitor.Unions.Select(f =>
                $"{spacing}UNION{(f.All ? " ALL" : string.Empty)}{Environment.NewLine}{spacing}{f.Sql}"));

            sql = $"{sql}{Environment.NewLine}{unions}";
        }

        if (visitor.IsAny)
        {
            sql = $"SELECT EXISTS({sql}) as result";
        }
        else if (visitor.IsAll)
        {
            sql = $"SELECT NOT EXISTS({sql}) as result";
        }

        return new SQLQuery
        {
            Sql = sql,
            Parameters = parameters,
            ThrowOnEmpty = visitor.ThrowOnEmpty,
            ThrowOnMoreThanOne = visitor.ThrowOnMoreThanOne,
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The type is an entity.")]
    private void HandleQueryableMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(Queryable.Select):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                visitor.TableColumns = visitor.ResolveResultAlias(lambda, lambda.Body);

                visitor.Selects.Clear();
                visitor.BuildSelect(lambda.Body, null);
                break;
            }
            case nameof(Queryable.Where):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                visitor.Wheres.Add(visitor.Visit(lambda.Body));
                break;
            }
            case nameof(Queryable.Join):
            case nameof(Queryable.GroupJoin):
            {
                Dictionary<string, ColumnMapping> newTableColumns;
                string alias;
                string sql;

                if (CommonHelpers.IsConstant(node.Arguments[1]))
                {
                    object? innerValue = CommonHelpers.GetConstantValue(node.Arguments[1]);

                    if (innerValue is SQLiteTable table)
                    {
                        Type entityType = table.ElementType;
                        alias = $"{entityType.Name.ToLowerInvariant()[..1]}{tableIndex.Index++}";
                        visitor.TableAliases[entityType] = alias;

                        TableMapping tableMapping = database.TableMapping(entityType);
                        newTableColumns = tableMapping.Columns
                            .ToDictionary(f => f.PropertyInfo.Name, f => new ColumnMapping(alias, f.Name, f.PropertyInfo.Name));
                        sql = $"\"{table.Table.TableName}\"";
                    }
                    else if (innerValue is IQueryable queryable)
                    {
                        SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
                        SQLQuery query = innerVisitor.Translate(queryable.Expression);

                        Type entityType = queryable.ElementType;
                        alias = $"{entityType.Name.ToLowerInvariant()[..1]}{tableIndex.Index++}";
                        visitor.TableAliases[entityType] = alias;

                        newTableColumns = entityType.GetProperties()
                            .ToDictionary(f => f.Name, f => new ColumnMapping(alias, f.Name, f.Name));
                        sql = $"({Environment.NewLine}{query.Sql}{Environment.NewLine})";
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

                    Type entityType = node.Arguments[1].Type.GetGenericArguments()[0];
                    alias = $"{entityType.Name.ToLowerInvariant()[..1]}{tableIndex.Index++}";
                    visitor.TableAliases[entityType] = alias;

                    newTableColumns = entityType.GetProperties()
                        .ToDictionary(f => f.Name, f => new ColumnMapping(alias, f.Name, f.Name));
                    sql = $"({Environment.NewLine}{query.Sql}{Environment.NewLine})";
                }
                else
                {
                    throw new NotSupportedException($"The type {node.Arguments[1].GetType().Name} is not supported in join.");
                }

                LambdaExpression outerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);
                LambdaExpression innerKey = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[3]);
                LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[4]);

                MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;
                MethodArguments[resultSelector.Parameters[1]] = newTableColumns;

                visitor.TableColumns = visitor.ResolveResultAlias(resultSelector, resultSelector.Body);

                MethodArguments[innerKey.Parameters[0]] = newTableColumns;

                string outerAlias = visitor.ResolveMember(outerKey.Body);
                string innerAlias = visitor.ResolveMember(innerKey.Body);

                visitor.Joins.Add(new JoinInfo
                {
                    JoinType = node.Method.Name == nameof(Queryable.GroupJoin) ? "LEFT JOIN" : "JOIN",
                    Sql = sql,
                    Alias = alias,
                    OnClause = $"{outerAlias} = {innerAlias}",
                    IsGroupJoin = node.Method.Name == nameof(Queryable.GroupJoin)
                });

                break;
            }
            case nameof(Queryable.SelectMany):
            {
                // TODO: This gets the last group join of a given type, but if we have two group joins of the same type it may not work.
                if (visitor.Joins.Count > 0)
                {
                    string alias = visitor.TableAliases[node.Method.GetGenericArguments()[^1]];

                    JoinInfo join = visitor.Joins.First(f => f.Alias == alias && f.IsGroupJoin);
                    join.JoinType = "LEFT JOIN";
                    join.IsGroupJoin = false;
                }

                LambdaExpression selector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                LambdaExpression resultSelector = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[2]);

                MethodArguments[resultSelector.Parameters[0]] = visitor.TableColumns;

                if (selector.Body is MethodCallExpression methodCallExpression && methodCallExpression.Arguments[0] is MemberExpression memberExpression)
                {
                    Dictionary<string, ColumnMapping> result = [];

                    foreach (KeyValuePair<string, ColumnMapping> tableColumn in visitor.TableColumns)
                    {
                        if (tableColumn.Key.StartsWith(memberExpression.Member.Name))
                        {
                            string path = tableColumn.Key[(memberExpression.Member.Name.Length + 1)..];
                            result.Add(path, tableColumn.Value);
                        }
                    }

                    MethodArguments[resultSelector.Parameters[1]] = result;
                }

                visitor.TableColumns = visitor.ResolveResultAlias(resultSelector, resultSelector.Body);
                break;
            }
            case nameof(Queryable.Take):
            {
                visitor.Take = (int)CommonHelpers.GetConstantValue(node.Arguments[1])!;
                break;
            }
            case nameof(Queryable.Skip):
            {
                visitor.Skip = (int)CommonHelpers.GetConstantValue(node.Arguments[1])!;
                break;
            }
            case nameof(Queryable.OrderBy):
            case nameof(Queryable.OrderByDescending):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                string orderBy = visitor.Visit(lambda.Body);

                visitor.OrderBys.Clear();
                visitor.OrderBys.Add(orderBy + (node.Method.Name == nameof(Queryable.OrderByDescending) ? " DESC" : string.Empty));
                break;
            }
            case nameof(Queryable.ThenBy):
            case nameof(Queryable.ThenByDescending):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                string orderBy = visitor.Visit(lambda.Body);
                visitor.OrderBys.Add(orderBy + (node.Method.Name == nameof(Queryable.ThenByDescending) ? " DESC" : string.Empty));
                break;
            }
            case nameof(Queryable.First):
            {
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.Take = 1;
                visitor.ThrowOnEmpty = true;
                break;
            }
            case nameof(Queryable.FirstOrDefault):
            {
                if (node.Arguments.Count == 2 && !CommonHelpers.IsConstant(node.Arguments[1]))
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.Take = 1;
                break;
            }
            case nameof(Queryable.Single):
            {
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.Take = 2;
                visitor.ThrowOnEmpty = true;
                visitor.ThrowOnMoreThanOne = true;
                break;
            }
            case nameof(Queryable.SingleOrDefault):
            {
                if (node.Arguments.Count == 2 && !CommonHelpers.IsConstant(node.Arguments[1]))
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.Take = 2;
                visitor.ThrowOnMoreThanOne = true;
                break;
            }
            case nameof(Queryable.Any):
            {
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.IsAny = true;
                break;
            }
            case nameof(Queryable.All):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                visitor.Wheres.Add(visitor.Visit(lambda.Body));

                visitor.IsAll = true;
                break;
            }
            case nameof(Queryable.Count):
            case nameof(Queryable.LongCount):
            {
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    visitor.Wheres.Add(visitor.Visit(lambda.Body));
                }

                visitor.Selects.Clear();
                visitor.Selects.Add("COUNT(*)");
                break;
            }
            case nameof(Queryable.Sum):
            {
                string sum;
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    sum = $"SUM({visitor.Visit(lambda.Body)})";
                }
                else if (visitor.Selects.Count == 1)
                {
                    sum = $"SUM({visitor.Selects[0]})";
                }
                else
                {
                    throw new NotSupportedException("A problem occured while compiling the SUM expression.");
                }

                visitor.Selects.Clear();
                visitor.Selects.Add(sum);
                break;
            }
            case nameof(Queryable.Max):
            {
                string max;
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    max = $"MAX({visitor.Visit(lambda.Body)})";
                }
                else if (visitor.Selects.Count == 1)
                {
                    max = $"MAX({visitor.Selects[0]})";
                }
                else
                {
                    throw new NotSupportedException("A problem occured while compiling the MAX expression.");
                }

                visitor.Selects.Clear();
                visitor.Selects.Add(max);
                break;
            }
            case nameof(Queryable.Min):
            {
                string min;
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    min = $"MIN({visitor.Visit(lambda.Body)})";
                }
                else if (visitor.Selects.Count == 1)
                {
                    min = $"MIN({visitor.Selects[0]})";
                }
                else
                {
                    throw new NotSupportedException("A problem occured while compiling the MIN expression.");
                }

                visitor.Selects.Clear();
                visitor.Selects.Add(min);
                break;
            }
            case nameof(Queryable.Average):
            {
                string avg;
                if (node.Arguments.Count == 2)
                {
                    LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                    avg = $"AVG({visitor.Visit(lambda.Body)})";
                }
                else if (visitor.Selects.Count == 1)
                {
                    avg = $"AVG({visitor.Selects[0]})";
                }
                else
                {
                    throw new NotSupportedException("A problem occured while compiling the AVG expression.");
                }

                visitor.Selects.Clear();
                visitor.Selects.Add(avg);
                break;
            }
            case nameof(Queryable.Distinct):
            {
                visitor.IsDistinct = true;
                break;
            }
            case nameof(Queryable.Concat):
            case nameof(Queryable.Union):
            {
                SQLTranslator translator = visitor.CloneDeeper(level);
                SQLQuery query = translator.Translate(node.Arguments[0]);

                visitor.Unions.Add((query.Sql, node.Method.Name == nameof(Queryable.Concat)));
                break;
            }
            case nameof(Queryable.Contains):
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

                string columnName = visitor.TableColumns.First().Value.Sql;

                string pName = $"@p{paramIndex.Index++}";
                parameters.Add(new SQLiteParameter
                {
                    Name = pName,
                    Value = value
                });
                visitor.Wheres.Add($"{columnName} = {pName}");
                visitor.IsAny = true;
                break;
            }
            case nameof(Queryable.GroupBy):
            {
                LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
                visitor.TableColumns = visitor.ResolveResultAlias(lambda, lambda.Body);

                // TODO: Add GroupBy support
                throw new NotSupportedException("GroupBy is not supported.");
            }
            default:
                throw new NotSupportedException($"Unsupported method: {node.Method}");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "We are checking the Queryable class")]
    private void TranslateMethodExpression(MethodCallExpression mce)
    {
        if (mce.Method.DeclaringType != typeof(Queryable) && mce.Method.DeclaringType != typeof(QueryableExtensions))
        {
            throw new NotSupportedException($"Unsupported method: {mce.Method}");
        }

        List<MethodCallExpression> methodCalls = [];
        MethodCallExpression callExpression = mce;

        while (true)
        {
            methodCalls.Add(callExpression);
            if (callExpression.Arguments.Count == 0
                || callExpression.Method.Name is nameof(Queryable.Concat) or nameof(Queryable.Union))
            {
                break;
            }

            if (callExpression.Arguments[0] is MethodCallExpression methodCall)
            {
                callExpression = methodCall;
            }
            else
            {
                break;
            }
        }

        if (callExpression.Arguments.Count == 0)
        {
            if (callExpression.Object!.Type.IsAssignableTo(typeof(SQLiteDatabase)))
            {
                visitor.AssignTable(callExpression.Method.GetGenericArguments()[0]);
                methodCalls.RemoveAt(methodCalls.Count - 1);
            }
            else
            {
                throw new NotSupportedException($"Unsupported method: {callExpression.Method}");
            }
        }
        else if (callExpression.Method.Name is nameof(Queryable.Concat) or nameof(Queryable.Union))
        {
            visitor.Visit(callExpression.Arguments[1]);
        }
        else
        {
            visitor.Visit(callExpression.Arguments[0]);
        }

        if (!IsSelectMethod(callExpression.Method.Name))
        {
            MethodCallExpression? lastResultMethodCall = Enumerable.Reverse(methodCalls)
                .FirstOrDefault(f => f.Arguments.OfType<UnaryExpression>().Any());
            Type genericType;

            if (lastResultMethodCall != null)
            {
                genericType = lastResultMethodCall.Method.GetGenericArguments()[^1];
            }
            else
            {
                genericType = CommonHelpers.GetQueryableType(callExpression.Arguments[0].Type)
                              ?? CommonHelpers.GetQueryableType(callExpression.Type)
                              ?? throw new InvalidOperationException("Expression is not an IQueryable.");
            }

            MethodCallExpression selectMethod = CreateIdentitySelectExpression(genericType);
            methodCalls.Insert(0, selectMethod);
        }

        for (int i = methodCalls.Count - 1; i >= 0; i--)
        {
            MethodCallExpression node = methodCalls[i];

            UnaryExpression? unaryExpression = node.Arguments
                .Skip(1)
                .OfType<UnaryExpression>()
                .FirstOrDefault();

            if (unaryExpression != null)
            {
                LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;
                visitor.MethodArguments[lambdaExpression.Parameters[0]] = visitor.TableColumns;
            }

            HandleQueryableMethodCall(node);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "We are checking the Queryable class")]
    private void TranslateOtherExpression(Expression expression)
    {
        Type genericType = expression.Type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>))
            ?.GenericTypeArguments[0] ?? throw new InvalidOperationException("Expression is not an IQueryable.");

        MethodCallExpression selectMethod = CreateIdentitySelectExpression(genericType);

        UnaryExpression unaryExpression = selectMethod.Arguments
            .Skip(1)
            .OfType<UnaryExpression>()
            .First();
        LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;

        visitor.Visit(expression);

        visitor.MethodArguments[lambdaExpression.Parameters[0]] = visitor.TableColumns;

        HandleQueryableMethodCall(selectMethod);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "We are checking the Queryable class")]
    private static MethodCallExpression CreateIdentitySelectExpression(Type genericType)
    {
        Type genericQueryableType = typeof(IQueryable<>).MakeGenericType(genericType);

        ParameterExpression sourceParameter = Expression.Parameter(genericQueryableType, "source");
        ParameterExpression elementParameter = Expression.Parameter(genericType, "x");

        LambdaExpression selector = Expression.Lambda(elementParameter, elementParameter);

        return Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            new[] { genericType, genericType },
            sourceParameter,
            selector
        );
    }

    private static bool IsSelectMethod(string methodName)
    {
        return methodName is nameof(Queryable.Select)
            or nameof(Queryable.Min)
            or nameof(Queryable.Max)
            or nameof(Queryable.Sum)
            or nameof(Queryable.Count)
            or nameof(Queryable.Average)
            or nameof(Queryable.Contains);
    }
}