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
    public List<IReadOnlyList<string>> SetOperandSelects { get; } = [];
    public List<SQLiteExpression> Selects { get; }
    public Dictionary<string, Type> SelectValueTypes { get; } = [];

    public long? Take { get; private set; }
    public long? Skip { get; private set; }
    public bool IsAny { get; private set; }
    public bool IsAll { get; private set; }
    public SQLiteExpression? AllPredicate { get; private set; }
    public bool Reverse { get; private set; }
    public bool IsDistinct { get; private set; }
    public bool ThrowOnEmpty { get; private set; }
    public bool ElementAtSemantic { get; private set; }
    public bool ThrowOnMoreThanOne { get; private set; }
    public object? DefaultValue { get; private set; }
    public bool HasDefaultValue { get; private set; }
    public bool IsRowSelector { get; private set; }
    public bool LastSelectIsClient { get; private set; }
    public bool ClientProjection { get; private set; }
    public bool ReverseBeforeDistinct { get; private set; }
    public long? ClientTake { get; private set; }
    public long? ClientSkip { get; private set; }
    public bool ClientCount { get; private set; }
    public bool SuppressSelectMaterializer { get; private set; }

    public bool IsInnerQuery { get; set; }

    public string? RawSelectSignature { get; private set; }
    public Expression? LastSelectLambdaBody { get; private set; }
    public Expression? JoinSelectExpression { get; private set; }

    public LambdaExpression? PreviousSelectLambda { get; set; }
    public Dictionary<string, Expression>? PreviousSelectSourceColumns { get; set; }

    public Expression Visit(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(System.Linq.Queryable.Select) => VisitSelect(node),
            nameof(System.Linq.Queryable.Where) => VisitWhere(node),
#if NET10_0_OR_GREATER
            nameof(System.Linq.Queryable.LeftJoin) => VisitJoin(node, "LEFT JOIN"),
            nameof(System.Linq.Queryable.RightJoin) => VisitJoin(node, "RIGHT JOIN"),
#endif
            nameof(System.Linq.Queryable.Join) => VisitJoin(node, "JOIN"),
            nameof(System.Linq.Queryable.GroupJoin) => VisitJoin(node, "LEFT JOIN"),
            nameof(QueryableExtensions.FullOuterJoin) => VisitJoin(node, "FULL OUTER JOIN"),
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
            nameof(System.Linq.Queryable.ElementAt) => VisitElementAt(node, throwOnEmpty: true),
            nameof(System.Linq.Queryable.ElementAtOrDefault) => VisitElementAt(node, throwOnEmpty: false),
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
            nameof(SQLiteDatabase.ValuesRange) => VisitValues(node),
            nameof(QueryableExtensions.GroupConcatMarker) => VisitGroupConcat(node),
            nameof(QueryableExtensions.TotalMarker) => VisitTotal(node),
            _ => throw new NotSupportedException($"Unsupported method: {node.Method}")
        };
    }

    private void ThrowIfSetOperations(string methodName)
    {
        if (SetOperations.Count > 0)
        {
            throw new NotSupportedException(
                $"{methodName} after Concat/Union/Intersect/Except is not supported because it would require wrapping the union in a subquery. " +
                "Apply the operation to each side before combining (e.g. `a.Where(p).Concat(b.Where(p))`).");
        }
    }



    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "Pragma entity types are rooted by user code.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    private (Dictionary<string, Expression> TableColmns, Type Type, SQLiteExpression Sql) ResolveTable(Expression body)
    {
        Dictionary<string, Expression> newTableColumns;
        Type entityType;
        SQLiteExpression sql;

        if (body is MethodCallExpression pragmaCall
            && pragmaCall.Method.GetCustomAttribute<SQLitePragmaFunctionAttribute>() is { } pragmaAttr)
        {
            entityType = pragmaCall.Method.ReturnType.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            TableMapping pragmaMapping = database.TableMapping(entityType);
            newTableColumns = pragmaMapping.Columns
                .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, visitor.Counters.NextIdentifier(), $"{alias}.\"{f.Name}\""));
            visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = alias };

            SQLiteExpression[] argExprs = new SQLiteExpression[pragmaCall.Arguments.Count];
            for (int i = 0; i < pragmaCall.Arguments.Count; i++)
            {
                argExprs[i] = (SQLiteExpression)visitor.Visit(pragmaCall.Arguments[i]);
            }

            SQLiteParameter[]? pragmaParams = ParameterHelpers.CombineParameters(argExprs);
            sql = SQLiteExpression.Variadic(body.Type, -1, $"{pragmaAttr.SqlName}(", argExprs, ", ", $") AS {alias}", pragmaParams);
        }
        else if (body is MethodCallExpression methodCall
                 && methodCall.Method.ReturnType.IsAssignableTo(typeof(BaseSQLiteTable)))
        {
            object? obj = methodCall.Object != null
                ? ExpressionHelpers.GetConstantValue(methodCall.Object!)
                : null;
            object?[]? methodArgs = methodCall.Arguments.Count == 0
                ? null
                : methodCall.Arguments.Select(ExpressionHelpers.GetConstantValue).ToArray();
            BaseSQLiteTable resultTable = (BaseSQLiteTable)methodCall.Method.Invoke(obj, methodArgs)!;

            entityType = resultTable.ElementType;
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            TableMapping tableMapping = resultTable.Table;
            newTableColumns = BuildMappedTableColumns(tableMapping, alias);
            visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = alias };
            sql = SQLiteExpression.Leaf(body.Type, -1, $"{visitor.QualifiedTableName(resultTable)} AS {alias}");
        }
        else if (ExpressionHelpers.IsConstant(body))
        {
            object? innerValue = ExpressionHelpers.GetConstantValue(body);

            if (innerValue is SQLiteCte cte)
            {
                visitor.CteRegistry ??= new CteRegistry();

                Type cteElementType = cte.ElementType;
                char cteAliasChar = char.ToLowerInvariant(cteElementType.Name.FirstOrDefault(char.IsLetter, 't'));
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
                    Expression cteBody = QueryFilterInjector.InjectCteBody(CommonHelpers.Inline(lambda.Body), visitor.Database, visitor.Counters);

                    if (isRecursive)
                    {
                        ParameterExpression selfParam = lambda.Parameters[0];

                        string placeholder = $"{cteAliasChar}__cte_self_{visitor.CteRegistry.Ctes.Count}__";

                        RecursiveCteBody recursive = visitor.TranslateRecursiveCteBody(cteElementType, placeholder, selfParam, cteBody);

                        string finalName = $"cte{visitor.CteRegistry.Ctes.Count}";
                        string fixedSql = recursive.Query.Sql.Replace(placeholder, finalName);

                        Dictionary<string, Expression>? recursiveNodes = CteColumnMapper.BodyConstructedNodes(recursive.Translator.Visitor);
                        cteName = visitor.CteRegistry.Register(
                            fixedSql,
                            recursive.Query.Parameters.ToArray(),
                            isRecursive: true,
                            key: cte,
                            columnNames: recursive.ColumnNames,
                            dayOfWeekColumns: recursive.DayOfWeekColumns,
                            jsonSourceColumns: recursive.JsonSourceColumns,
                            constructedPaths: CteColumnMapper.BodyConstructedPaths(recursive.Translator.Visitor),
                            constructedNodes: recursiveNodes,
                            bodyColumns: recursive.HasClientMember ? recursive.Translator.Visitor.TableColumns : null,
                            bodySelects: recursive.HasClientMember || recursiveNodes != null ? recursive.Translator.Selects : null,
                            emittedColumns: CteColumnMapper.EmittedColumnNames(recursive.ColumnNames, recursive.Translator.Selects),
                            optionalRow: recursive.Translator.Visitor.OptionalRowColumns.Contains(recursive.Translator.Visitor.TableColumns),
                            optionalRowPaths: recursive.Translator.Visitor.OptionalRowPaths.TryGetValue(recursive.Translator.Visitor.TableColumns, out HashSet<string>? recursiveOptionalPaths)
                                ? recursiveOptionalPaths
                                : null);

                        visitor.CteParameters.Remove(selfParam);
                        visitor.MethodArguments.Remove(selfParam);
                    }
                    else
                    {
                        SQLTranslator bodyTranslator = visitor.CloneDeeper(visitor.Level + 1);
                        SQLQuery bodyQuery = bodyTranslator.Translate(cteBody);

                        if (bodyQuery.Reverse || bodyQuery.ReverseBeforeDistinct)
                        {
                            throw new NotSupportedException(
                                "The common table expression body ends with Reverse(), which only runs in memory after the query returns, " +
                                "so the expression cannot keep that order. Use OrderByDescending instead.");
                        }

                        string[]? bodyColumnNames = CteColumnMapper.DeclaredColumnNames(
                            cteElementType, bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects, database.Options);
                        bool hasClientMember = CteColumnMapper.HasClientBodyMember(bodyTranslator.Visitor.TableColumns)
                            || CteColumnMapper.BodyColumnOrderIsAmbiguous(bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects);
                        Dictionary<string, Expression>? bodyNodes = CteColumnMapper.BodyConstructedNodes(bodyTranslator.Visitor);
                        cteName = visitor.CteRegistry.Register(
                            bodyQuery.Sql,
                            bodyQuery.Parameters.ToArray(),
                            isRecursive: false,
                            key: cte,
                            columnNames: bodyColumnNames,
                            dayOfWeekColumns: CteColumnMapper.DayOfWeekColumns(bodyTranslator.Visitor.TableColumns, TypeHelpers.IsSimple(cteElementType, database.Options)),
                            jsonSourceColumns: CteColumnMapper.JsonSourceColumns(bodyTranslator.Visitor.TableColumns, TypeHelpers.IsSimple(cteElementType, database.Options)),
                            constructedPaths: CteColumnMapper.BodyConstructedPaths(bodyTranslator.Visitor),
                            constructedNodes: bodyNodes,
                            bodyColumns: hasClientMember ? bodyTranslator.Visitor.TableColumns : null,
                            bodySelects: hasClientMember || bodyNodes != null ? bodyTranslator.Selects : null,
                            emittedColumns: CteColumnMapper.EmittedColumnNames(bodyColumnNames, bodyTranslator.Selects),
                            optionalRow: bodyTranslator.Visitor.OptionalRowColumns.Contains(bodyTranslator.Visitor.TableColumns),
                            optionalRowPaths: bodyTranslator.Visitor.OptionalRowPaths.TryGetValue(bodyTranslator.Visitor.TableColumns, out HashSet<string>? bodyOptionalPaths)
                                ? bodyOptionalPaths
                                : null);
                    }
                }

                entityType = cteElementType;
                CteInfo cteInfo = visitor.CteRegistry.Info(cte);
                newTableColumns = CteColumnMapper.BuildOuterColumns(cteInfo, cteElementType, cteAlias, database.Options, visitor.Counters);
                CteColumnMapper.ApplyBodyTraits(newTableColumns, cteInfo, visitor, cteAlias);
                visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = cteAlias };
                sql = SQLiteExpression.Leaf(body.Type, -1, $"{cteName} AS {cteAlias}");
            }
            else if (innerValue is BaseSQLiteTable table)
            {
                entityType = table.ElementType;
                char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
                string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

                TableMapping tableMapping = table.Table;
                newTableColumns = BuildMappedTableColumns(tableMapping, alias);
                visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = alias };
                sql = SQLiteExpression.Leaf(body.Type, -1, $"{visitor.QualifiedTableName(table)} AS {alias}");
            }
            else
            {
                throw new NotSupportedException($"The type {innerValue!.GetType().Name} is not supported in join.");
            }
        }
        else if (body is ParameterExpression paramBody && visitor.CteParameters.TryGetValue(paramBody, out CteSelfReference? cteParamRef))
        {
            entityType = body.Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            newTableColumns = CteColumnMapper.BuildSelfColumns(cteParamRef, alias, database.Options, visitor.Counters, visitor);
            visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = alias };
            sql = SQLiteExpression.Leaf(body.Type, -1, $"{cteParamRef.Placeholder} AS {alias}");
        }
        else if (TryGetQueryableElementType(body.Type) is { } queryableElementType)
        {
            SQLTranslator innerVisitor = visitor.CloneDeeper(visitor.Level + 1);
            SQLQuery query = innerVisitor.Translate(body);

            if (innerVisitor.ClientProjection || innerVisitor.LastSelectIsClient)
            {
                throw new NotSupportedException(
                    "A join or SelectMany source that ends in a Select that runs in memory is not supported, " +
                    "because SQLite cannot read values the database never computes. Apply the projection after combining the sources.");
            }

            entityType = queryableElementType;
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

            if (TypeHelpers.IsSimple(entityType, database.Options) && innerVisitor.Selects.Count == 1)
            {
                KeyValuePair<string, Expression> shape = innerVisitor.Visitor.TableColumns.First();
                string columnName = innerVisitor.Selects[0].IdentifierText;
                SQLiteExpression scalarLeaf = SQLiteExpression.Leaf(entityType, visitor.Counters.NextIdentifier(), $"{alias}.\"{columnName}\"");
                if (innerVisitor.Selects[0].IsDayOfWeekInteger)
                {
                    scalarLeaf.WithDayOfWeekInteger();
                }

                if (innerVisitor.Selects[0].IsJsonSource)
                {
                    scalarLeaf.WithJsonSource();
                }

                newTableColumns = new Dictionary<string, Expression>
                {
                    [shape.Key] = scalarLeaf
                };
            }
            else
            {
                newTableColumns = [];
                foreach (SQLiteExpression select in innerVisitor.Selects)
                {
                    string columnName = select.IdentifierText;
                    if (newTableColumns.ContainsKey(columnName))
                    {
                        continue;
                    }

                    Type columnType = query.SelectValueTypes!.GetValueOrDefault(columnName, select.Type);
                    SQLiteExpression leaf = SQLiteExpression.Leaf(columnType, visitor.Counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(columnName)}");
                    if (select.IsDayOfWeekInteger)
                    {
                        leaf.WithDayOfWeekInteger();
                    }

                    if (select.IsJsonSource)
                    {
                        leaf.WithJsonSource();
                    }

                    newTableColumns[columnName] = leaf;
                }
            }

            if (innerVisitor.Visitor.OptionalRowColumns.Contains(innerVisitor.Visitor.TableColumns))
            {
                visitor.OptionalRowColumns.Add(newTableColumns);
            }

            if (innerVisitor.Visitor.OptionalRowPaths.TryGetValue(innerVisitor.Visitor.TableColumns, out HashSet<string>? innerOptionalPaths))
            {
                visitor.OptionalRowPaths[newTableColumns] = [.. innerOptionalPaths];
            }

            if (innerVisitor.Visitor.ConstructedProjectionPaths.TryGetValue(innerVisitor.Visitor.TableColumns, out HashSet<string>? innerConstructedPaths))
            {
                visitor.ConstructedProjectionPaths[newTableColumns] = [.. innerConstructedPaths];
            }

            visitor.TableColumnPrefixes[newTableColumns] = new Dictionary<string, string?> { [string.Empty] = alias };
            sql = SQLiteExpression.Leaf(
                body.Type,
                -1,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine}) AS {alias}",
                query.Parameters.Count != 0 ? query.Parameters.ToArray() : null
            );
        }
        else if (body is MemberExpression jsonMember && database.Options.HasJsonConverter(jsonMember.Type))
        {
            throw new NotSupportedException(
                $"SelectMany over the JSON collection column '{jsonMember.Member.Name}' is not supported at the query level.");
        }
        else
        {
            throw new NotSupportedException($"The type {body.GetType().Name} is not supported in join.");
        }

        return (newTableColumns, entityType, sql);
    }

    private Dictionary<string, Expression> BuildMappedTableColumns(TableMapping tableMapping, string alias)
    {
        return tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) =>
            {
                string colSql = $"{alias}.{IdentifierGuard.Quote(f.Name)}";
                if (database.Options.TypeConverters.TryGetValue(f.PropertyType, out ISQLiteTypeConverter? converter)
                    && converter.ColumnSqlExpression is { } columnSqlExpression)
                {
                    colSql = string.Format(columnSqlExpression, colSql);
                }

                return SQLiteExpression.Leaf(f.PropertyType, visitor.Counters.NextIdentifier(), colSql);
            });
    }

    private static bool ContainsClientCall(Expression node)
    {
        ClientCallFinder finder = new();
        finder.Visit(node);
        return finder.Found;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Reads the IQueryable<> interface only.")]
    private static Type? TryGetQueryableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            return type.GetGenericArguments()[0];
        }

        Type? queryableInterface = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>));
        return queryableInterface?.GetGenericArguments()[0];
    }
}
