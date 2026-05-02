namespace SQLite.Framework.Internals.Visitors.Queryable;

/// <summary>
/// Goes through the LINQ methods and gets the different SQL query parts from them.
/// </summary>
internal partial class QueryableVisitor
{
    private readonly AliasVisitor aliasVisitor;
    private readonly SQLiteDatabase database;
    private readonly SelectVisitor selectVisitor;
    private readonly SQLVisitor visitor;

    public QueryableVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        Selects = [];
        selectVisitor = new SelectVisitor(Selects);
        aliasVisitor = new AliasVisitor(database, visitor);
    }

    public List<JoinInfo> Joins { get; } = [];
    public List<SQLiteExpression> Wheres { get; } = [];
    public List<SQLiteExpression> OrderBys { get; } = [];
    public List<SQLiteExpression> GroupBys { get; } = [];
    public List<SQLiteExpression> Havings { get; } = [];
    public List<(SQLiteExpression Sql, string Type)> SetOperations { get; } = [];
    public List<SQLiteExpression> Selects { get; }

    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsAny { get; private set; }
    public bool IsAll { get; private set; }
    public bool Reverse { get; private set; }
    public bool IsDistinct { get; private set; }
    public bool ThrowOnEmpty { get; private set; }
    public bool ThrowOnMoreThanOne { get; private set; }

    public bool IsInnerQuery { get; set; }

    public string? RawSelectSignature { get; private set; }
    public Expression? LastSelectLambdaBody { get; private set; }

    public LambdaExpression? PreviousSelectLambda { get; set; }

    public Expression Visit(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(System.Linq.Queryable.Select) => VisitSelect(node),
            nameof(System.Linq.Queryable.Where) => VisitWhere(node),
#if NET10_0
            nameof(System.Linq.Queryable.LeftJoin) => VisitJoin(node, "LEFT JOIN"),
            nameof(System.Linq.Queryable.RightJoin) => VisitJoin(node, "JOIN"),
#endif
            nameof(System.Linq.Queryable.Join) => VisitJoin(node, "JOIN"),
            nameof(System.Linq.Queryable.GroupJoin) => VisitJoin(node, "LEFT JOIN"),
            nameof(System.Linq.Queryable.SelectMany) => VisitSelectMany(node),
            nameof(System.Linq.Queryable.Take) => VisitTake(node),
            nameof(System.Linq.Queryable.Skip) => VisitSkip(node),
            nameof(System.Linq.Queryable.OrderBy) => VisitOrder(node),
            nameof(System.Linq.Queryable.OrderByDescending) => VisitOrder(node),
            nameof(System.Linq.Queryable.ThenBy) => VisitOrder(node),
            nameof(System.Linq.Queryable.ThenByDescending) => VisitOrder(node),
            nameof(System.Linq.Queryable.First) => VisitScalar(node),
            nameof(System.Linq.Queryable.FirstOrDefault) => VisitScalar(node),
            nameof(System.Linq.Queryable.Single) => VisitScalar(node),
            nameof(System.Linq.Queryable.SingleOrDefault) => VisitScalar(node),
            nameof(System.Linq.Queryable.Any) => VisitBoolean(node),
            nameof(System.Linq.Queryable.All) => VisitBoolean(node),
            nameof(System.Linq.Queryable.Count) or nameof(System.Linq.Queryable.LongCount) => VisitGroupFunction(node, "COUNT"),
            nameof(System.Linq.Queryable.Sum) => VisitGroupFunction(node, "SUM"),
            nameof(System.Linq.Queryable.Max) => VisitGroupFunction(node, "MAX"),
            nameof(System.Linq.Queryable.Min) => VisitGroupFunction(node, "MIN"),
            nameof(System.Linq.Queryable.Average) => VisitGroupFunction(node, "AVG"),
            nameof(System.Linq.Queryable.Distinct) => VisitDistinct(node),
            nameof(System.Linq.Queryable.Concat) => VisitSetOperation(node, "UNION ALL"),
            nameof(System.Linq.Queryable.Union) => VisitSetOperation(node, "UNION"),
            nameof(System.Linq.Queryable.Intersect) => VisitSetOperation(node, "INTERSECT"),
            nameof(System.Linq.Queryable.Except) => VisitSetOperation(node, "EXCEPT"),
            nameof(System.Linq.Queryable.Contains) => VisitContains(node),
            nameof(System.Linq.Queryable.GroupBy) => VisitGroupBy(node),
            nameof(System.Linq.Queryable.Reverse) => VisitReverse(node),
            nameof(System.Linq.Queryable.Cast) => node,
            nameof(SQLiteDatabase.FromSql) => VisitFromSql(node),
            nameof(SQLiteDatabase.Values) => VisitValues(node),
            _ => throw new NotSupportedException($"Unsupported method: {node.Method}")
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    private (Dictionary<string, Expression> TableColmns, Type Type, SQLiteExpression Sql) ResolveTable(Expression body)
    {
        Dictionary<string, Expression> newTableColumns;
        Type entityType;
        SQLiteExpression sql;

        if (body is MethodCallExpression methodCall && methodCall.Method.ReturnType.IsAssignableTo(typeof(BaseSQLiteTable)))
        {
            object? obj = methodCall.Object != null
                ? ExpressionHelpers.GetConstantValue(methodCall.Object!)
                : null;
            BaseSQLiteTable resultTable = (BaseSQLiteTable)methodCall.Method.Invoke(obj, null)!;

            entityType = resultTable.ElementType;
            char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            TableMapping tableMapping = database.TableMapping(entityType);
            newTableColumns = tableMapping.Columns
                .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLiteExpression(f.PropertyType, visitor.Counters.IdentifierIndex++, $"{alias}.{f.Name}"));
            sql = new SQLiteExpression(body.Type, -1, $"\"{tableMapping.TableName}\" AS {alias}");
        }
        else if (ExpressionHelpers.IsConstant(body))
        {
            object? innerValue = ExpressionHelpers.GetConstantValue(body);

            if (innerValue is SQLiteCte cte)
            {
                visitor.CteRegistry ??= new CteRegistry();

                Type cteElementType = cte.ElementType;
                char cteAliasChar = char.ToLowerInvariant(cteElementType.Name[0]);
                string cteAlias = $"{cteAliasChar}{visitor.Counters.NextTableIndex(cteAliasChar)}";

                string? cachedName = visitor.CteRegistry.TryGetName(cte);
                string cteName;

                if (cachedName != null)
                {
                    cteName = cachedName;
                }
                else
                {
                    LambdaExpression lambda = cte.Query;
                    bool isRecursive = lambda.Parameters.Count == 1;

                    if (isRecursive)
                    {
                        ParameterExpression selfParam = lambda.Parameters[0];

                        string placeholder = $"{cteAliasChar}__cte_self_{visitor.CteRegistry.Ctes.Count}__";

                        Dictionary<string, Expression> selfColumns = cteElementType.GetProperties()
                            .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, visitor.Counters.IdentifierIndex++, $"{placeholder}.{f.Name}"));

                        visitor.CteParameters[selfParam] = (placeholder, selfColumns);
                        visitor.MethodArguments[selfParam] = selfColumns;

                        SQLTranslator bodyTranslator = visitor.CloneDeeper(visitor.Level + 1);
                        SQLQuery bodyQuery = bodyTranslator.Translate(lambda.Body);

                        string finalName = $"cte{visitor.CteRegistry.Ctes.Count}";
                        string fixedSql = bodyQuery.Sql.Replace(placeholder, finalName);

                        cteName = visitor.CteRegistry.Register(fixedSql, bodyQuery.Parameters.ToArray(), isRecursive: true, key: cte);

                        visitor.CteParameters.Remove(selfParam);
                        visitor.MethodArguments.Remove(selfParam);
                    }
                    else
                    {
                        SQLTranslator bodyTranslator = visitor.CloneDeeper(visitor.Level + 1);
                        SQLQuery bodyQuery = bodyTranslator.Translate(lambda.Body);

                        cteName = visitor.CteRegistry.Register(bodyQuery.Sql, bodyQuery.Parameters.ToArray(), isRecursive: false, key: cte);
                    }
                }

                entityType = cteElementType;
                newTableColumns = cteElementType.GetProperties()
                    .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, visitor.Counters.IdentifierIndex++, $"{cteAlias}.{f.Name}"));
                sql = new SQLiteExpression(body.Type, -1, $"{cteName} AS {cteAlias}");
            }
            else if (innerValue is SQLiteTable table)
            {
                entityType = table.ElementType;
                char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
                string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

                TableMapping tableMapping = database.TableMapping(entityType);
                newTableColumns = tableMapping.Columns
                    .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLiteExpression(f.PropertyType, visitor.Counters.IdentifierIndex++, $"{alias}.{f.Name}"));
                sql = new SQLiteExpression(body.Type, -1, $"\"{table.Table.TableName}\" AS {alias}");
            }
            else
            {
                throw new NotSupportedException($"The type {innerValue!.GetType().Name} is not supported in join.");
            }
        }
        else if (body is ParameterExpression paramBody && visitor.CteParameters.TryGetValue(paramBody, out (string Alias, Dictionary<string, Expression> Columns) cteParamRef))
        {
            entityType = body.Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            newTableColumns = cteParamRef.Columns
                .ToDictionary(kv => kv.Key, Expression (kv) => new SQLiteExpression(
                    ((SQLiteExpression)kv.Value).Type,
                    visitor.Counters.IdentifierIndex++,
                    $"{alias}.{kv.Key}"));
            sql = new SQLiteExpression(body.Type, -1, $"{cteParamRef.Alias} AS {alias}");
        }
        else if (body.Type.IsGenericType && body.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
            SQLQuery query = innerVisitor.Translate(body);

            entityType = body.Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            newTableColumns = entityType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, visitor.Counters.IdentifierIndex++, $"{alias}.{f.Name}"));
            sql = new SQLiteExpression(
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
