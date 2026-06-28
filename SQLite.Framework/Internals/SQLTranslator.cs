namespace SQLite.Framework.Internals;

/// <summary>
/// Translates LINQ expressions into SQL queries.
/// </summary>
/// <remarks>
/// This class gets the different parts of the LINQ expression tree and translates them into SQL.
/// </remarks>
internal class SQLTranslator
{
    private readonly SQLiteDatabase database;
    private readonly int level;
    private readonly bool isInnerQuery;
    private readonly List<SQLiteParameter> parameters = [];
    private readonly QueryableVisitor queryableMethodVisitor;
    private Expression? selectMethodExpression;
    private bool skipGeneratedMaterializers;

    public SQLTranslator(SQLiteDatabase database)
    {
        this.database = database;
        SQLiteCounters counters = new();

        Visitor = new SQLVisitor(database, counters, level);
        queryableMethodVisitor = new QueryableVisitor(database, Visitor);
    }

    public SQLTranslator(SQLiteDatabase database, SQLiteCounters counters, int level, bool isInnerQuery)
    {
        this.database = database;
        this.level = level;
        this.isInnerQuery = isInnerQuery;
        Visitor = new SQLVisitor(database, counters, level);
        queryableMethodVisitor = new QueryableVisitor(database, Visitor)
        {
            IsInnerQuery = isInnerQuery
        };
    }

    public SQLVisitor Visitor { get; }

    public IReadOnlyList<SQLiteExpression> Selects => queryableMethodVisitor.Selects;

    public bool HasTopLevelOrderingOrPaging =>
        queryableMethodVisitor.OrderBys.Count > 0
        || queryableMethodVisitor.Take != null
        || queryableMethodVisitor.Skip != null;

    public bool HasSetOperations => queryableMethodVisitor.SetOperations.Count > 0;

    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments
    {
        init => Visitor.MethodArguments = value;
    }

    public CteRegistry? CteRegistry
    {
        init => Visitor.CteRegistry = value;
    }

    public Dictionary<ParameterExpression, (string Alias, Dictionary<string, Expression> Columns)> CteParameters
    {
        init => Visitor.CteParameters = value;
    }

    public Dictionary<Dictionary<string, Expression>, Dictionary<string, string?>> TableColumnPrefixes
    {
        init => Visitor.TableColumnPrefixes = value;
    }

    public QueryType QueryType { get; init; }

    public bool EmitReturning { get; init; }

    public bool OmitTableAlias
    {
        init => Visitor.OmitTableAlias = value;
    }

    public List<(string Name, SQLiteExpression Expression)>? SetProperties { get; set; }

    public void Visit(Expression node)
    {
        node = CommonHelpers.Inline(node);

        if (!isInnerQuery)
        {
            bool ignoreAll = QueryFilterInjector.ShouldIgnoreAll(node, Visitor.Database.Options);
            Visitor.Counters.IgnoreQueryFilters = ignoreAll;
            node = QueryFilterInjector.Inject(node, Visitor.Database.Options, ignoreAll);
        }
        if (node is MethodCallExpression mce)
        {
            selectMethodExpression = TranslateMethodExpression(mce);
        }
        else
        {
            selectMethodExpression = TranslateOtherExpression(node);
        }
    }

    public SQLQuery Translate(Expression? node)
    {
        if (node != null)
        {
            Visit(node);
        }

        if (Visitor.From == null)
        {
            throw new InvalidOperationException("Could not identify FROM clause.");
        }

        CteRegistry? cteRegistry = Visitor.CteRegistry;

        string spacing = new(' ', level * 4);

        if (queryableMethodVisitor.Joins.Any(f => f.IsGroupJoin))
        {
            throw new NotSupportedException(
                "Group joins are only supported when flattened into a LEFT JOIN. " +
                "Use the pattern `from x in table join y in other on x.Id equals y.Id into g from y in g.DefaultIfEmpty() select ...` " +
                "so the framework can translate it to a LEFT JOIN.");
        }

        bool useExists = queryableMethodVisitor.IsAny || queryableMethodVisitor.IsAll;
        bool hasSetOperations = queryableMethodVisitor.SetOperations.Count > 0;

        // Visit all expressions to collect parameters before composing SQL.
        if (QueryType == QueryType.Select && (!useExists || hasSetOperations))
        {
            foreach (SQLiteExpression expression in queryableMethodVisitor.Selects)
            {
                VisitSQLExpression(expression);
            }
        }

        VisitSQLExpression(Visitor.From);

        if (QueryType == QueryType.Update)
        {
            foreach ((string _, SQLiteExpression sqlExpression) in SetProperties!)
            {
                VisitSQLExpression(sqlExpression);
            }
        }

        foreach (SQLiteExpression sqlExpression in queryableMethodVisitor.Wheres)
        {
            VisitSQLExpression(sqlExpression);
        }

        if (queryableMethodVisitor.AllPredicate != null)
        {
            VisitSQLExpression(queryableMethodVisitor.AllPredicate);
        }

        foreach (JoinInfo join in queryableMethodVisitor.Joins)
        {
            VisitSQLExpression(join.Sql);

            if (join.OnClause != null)
            {
                VisitSQLExpression(join.OnClause);
            }
        }

        foreach (SQLiteExpression sqlExpression in queryableMethodVisitor.GroupBys)
        {
            VisitSQLExpression(sqlExpression);
        }

        foreach (SQLiteExpression sqlExpression in queryableMethodVisitor.Havings)
        {
            VisitSQLExpression(sqlExpression);
        }

        foreach (SQLiteExpression sqlExpression in queryableMethodVisitor.OrderBys)
        {
            VisitSQLExpression(sqlExpression);
        }

        if (hasSetOperations)
        {
            foreach ((SQLiteExpression sqlExpression, string _) in queryableMethodVisitor.SetOperations)
            {
                VisitSQLExpression(sqlExpression);
            }
        }

        StringBuilder sb = StringBuilderPool.Rent();
        WriteQuerySql(sb, queryableMethodVisitor, spacing, useExists, hasSetOperations);

        string sql = StringBuilderPool.ToStringAndReturn(sb);

        if (!isInnerQuery)
        {
            if (queryableMethodVisitor.IsAny)
            {
                sql = $"{spacing}SELECT EXISTS({sql.Trim()}) as result";
            }
            else if (queryableMethodVisitor.IsAll)
            {
                sql = $"{spacing}SELECT NOT EXISTS({sql.Trim()}) as result";
            }
        }

        if (level == 0 && !isInnerQuery && cteRegistry?.Ctes.Count > 0)
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_8_3, "Common table expressions (WITH ... AS)");
#endif
            bool anyRecursive = cteRegistry.Ctes.Any(c => c.IsRecursive);
            string withKeyword = anyRecursive ? "WITH RECURSIVE" : "WITH";

            StringBuilder cteSb = StringBuilderPool.Rent();
            cteSb.Append(withKeyword);
            cteSb.Append(' ');
            for (int i = 0; i < cteRegistry.Ctes.Count; i++)
            {
                if (i > 0)
                {
                    cteSb.Append(',');
                    cteSb.Append(Environment.NewLine);
                }
                CteInfo cte = cteRegistry.Ctes[i];
                cteSb.Append(cte.Name);
                if (cte.ColumnNames != null)
                {
                    cteSb.Append('(');
                    cteSb.Append(string.Join(", ", cte.ColumnNames.Select(IdentifierGuard.Quote)));
                    cteSb.Append(')');
                }
                cteSb.Append(" AS ");
#if SQLITE_FRAMEWORK_VERSION_AWARE
                if (cte.Materialization != SQLiteCteMaterialization.Default)
                {
                    database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "MATERIALIZED CTE hints");
                }
#endif
                switch (cte.Materialization)
                {
                    case SQLiteCteMaterialization.Materialized:
                        cteSb.Append("MATERIALIZED ");
                        break;
                    case SQLiteCteMaterialization.NotMaterialized:
                        cteSb.Append("NOT MATERIALIZED ");
                        break;
                }
                cteSb.Append('(');
                cteSb.Append(Environment.NewLine);
                cteSb.Append(cte.Sql);
                cteSb.Append(Environment.NewLine);
                cteSb.Append(')');
            }
            cteSb.Append(Environment.NewLine);
            cteSb.Append(sql);
            sql = StringBuilderPool.ToStringAndReturn(cteSb);

            foreach (CteInfo cte in cteRegistry.Ctes)
            {
                if (cte.Parameters != null)
                {
                    parameters.InsertRange(0, cte.Parameters);
                }
            }
        }

        Func<SQLiteQueryContext, object?>? createObject;
        IReadOnlyList<MethodInfo>? reflectedMethods = null;
        IReadOnlyList<object?>? reflectedInstances = null;
        IReadOnlyList<object?>? capturedValues = null;
        IReadOnlyList<Type>? reflectedTypes = null;
        IReadOnlyList<MemberInfo>? reflectedMembers = null;
        IReadOnlyList<ConstructorInfo>? reflectedConstructors = null;
        IReadOnlyDictionary<string, Func<SQLiteQueryContext, object?>> selectMaterializers2 = database.Options.SelectMaterializers;
        string? rawSignature2 = queryableMethodVisitor.RawSelectSignature;
        bool hasReflectedArg = queryableMethodVisitor.LastSelectLambdaBody is NewExpression ne
           && ne.Type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal)
           && ne.Arguments.Any(a => !a.Type.IsVisible);

        if (!skipGeneratedMaterializers
            && selectMaterializers2.Count > 0
            && hasReflectedArg
            && rawSignature2 != null
            && selectMaterializers2.TryGetValue(rawSignature2, out Func<SQLiteQueryContext, object?>? generatedAnon))
        {
#if SQLITE_FRAMEWORK_TESTING
            database.IncrementSelectMaterializerHits();
#endif
            createObject = generatedAnon;
            ReflectedBindingsCollector anonCollector = new();
            anonCollector.Visit(queryableMethodVisitor.LastSelectLambdaBody);
            reflectedMethods = anonCollector.Methods;
            reflectedInstances = anonCollector.Instances;
            capturedValues = anonCollector.CapturedValues;
            reflectedTypes = anonCollector.Types;
            reflectedMembers = anonCollector.Members;
            reflectedConstructors = anonCollector.Constructors;
        }
        else if (UsesEntityMaterializer(selectMethodExpression))
        {
            createObject = null;
        }
        else
        {
            IReadOnlyDictionary<string, Func<SQLiteQueryContext, object?>> selectMaterializers = database.Options.SelectMaterializers;
            string? rawSignature = queryableMethodVisitor.RawSelectSignature;
            if (!skipGeneratedMaterializers
                && selectMaterializers.Count > 0
                && rawSignature != null
                && selectMaterializers.TryGetValue(rawSignature, out Func<SQLiteQueryContext, object?>? generated))
            {
#if SQLITE_FRAMEWORK_TESTING
                database.IncrementSelectMaterializerHits();
#endif
                createObject = generated;

                ReflectedBindingsCollector collector = new();
                collector.Visit(selectMethodExpression!);
                reflectedMethods = collector.Methods;
                reflectedInstances = collector.Instances;
                capturedValues = collector.CapturedValues;
                reflectedTypes = collector.Types;
                reflectedMembers = collector.Members;
                reflectedConstructors = collector.Constructors;
            }
            else
            {
                if (rawSignature != null && database.Options.ReflectionFallbackDisabled && !skipGeneratedMaterializers)
                {
                    throw new InvalidOperationException(
                        "Select projection fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                        "The source generator did not cover this projection shape. " +
                        $"Projection signature: {rawSignature}. " +
                        "Either install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers, " +
                        "change the Select to a shape the generator supports, " +
                        "or remove the DisableReflectionFallback call.");
                }
#if SQLITE_FRAMEWORK_TESTING
                if (rawSignature != null)
                {
                    database.IncrementSelectCompilerFallbacks();
                }
#endif
                QueryCompilerVisitor compilerVisitor = new(database.Options);
                CompiledExpression compiledExpression = (CompiledExpression)compilerVisitor.Visit(selectMethodExpression!);
                createObject = compiledExpression.Call;
            }
        }

        return new SQLQuery
        {
            Sql = sql,
            Parameters = parameters,
            CreateObject = createObject,
            Reverse = queryableMethodVisitor.Reverse,
            ThrowOnEmpty = queryableMethodVisitor.ThrowOnEmpty,
            ElementAtSemantic = queryableMethodVisitor.ElementAtSemantic,
            ThrowOnMoreThanOne = queryableMethodVisitor.ThrowOnMoreThanOne,
            DefaultValue = queryableMethodVisitor.DefaultValue,
            HasDefaultValue = queryableMethodVisitor.HasDefaultValue,
            IsRowSelector = queryableMethodVisitor.IsRowSelector,
            ReflectedMethods = reflectedMethods,
            ReflectedMethodInstances = reflectedInstances,
            CapturedValues = capturedValues,
            ReflectedTypes = reflectedTypes,
            ReflectedMembers = reflectedMembers,
            ReflectedConstructors = reflectedConstructors,
        };
    }

    private void VisitSQLExpression(SQLiteExpression node)
    {
        if (node.Parameters != null)
        {
            parameters.AddRange(node.Parameters);
        }
    }

    private void WriteQuerySql(StringBuilder sb, Visitors.Queryable.QueryableVisitor q, string spacing, bool useExists, bool hasSetOperations)
    {
        bool first = true;

        if (QueryType is QueryType.Delete or QueryType.Update && (q.Take != null || q.Skip != null))
        {
            throw new NotSupportedException(
                $"{QueryType} with Take or Skip is not supported, because SQLite does not allow a row limit on DELETE or UPDATE.");
        }

        // SELECT
        if (QueryType == QueryType.Select)
        {
            sb.Append(spacing);
            sb.Append("SELECT");
            if (q.IsDistinct) sb.Append(" DISTINCT");
            sb.Append(' ');
            if (useExists && !hasSetOperations)
            {
                sb.Append('1');
            }
            else if (q.Selects.Count > 0)
            {
                for (int i = 0; i < q.Selects.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                        sb.Append(Environment.NewLine);
                        sb.Append("       ");
                    }
                    q.Selects[i].WriteSqlTo(sb);
                    sb.Append(" AS \"");
                    sb.Append(q.Selects[i].IdentifierText);
                    sb.Append('"');
                }
            }
            else
            {
                sb.Append('*');
            }
            first = false;
        }

        // FROM (or DELETE FROM, UPDATE)
        AppendSpacingNewline(sb, spacing, ref first);
        switch (QueryType)
        {
            case QueryType.Delete:
                sb.Append("DELETE FROM ");
                break;
            case QueryType.Update:
                sb.Append("UPDATE ");
                break;
            default:
                sb.Append("FROM ");
                break;
        }
        Visitor.From!.WriteSqlTo(sb);

        bool isUpdateFrom = QueryType == QueryType.Update && q.Joins.Count > 0;

        if (QueryType == QueryType.Delete && q.Joins.Count > 0)
        {
            throw new NotSupportedException(
                "ExecuteDelete does not support a join. Filter the rows to delete with a Where predicate instead of joining.");
        }

        if (isUpdateFrom)
        {
            foreach (JoinInfo join in q.Joins)
            {
                if (join.JoinType == "LEFT JOIN")
                {
                    throw new NotSupportedException(
                        "ExecuteUpdate does not support a left join. Only inner joins translate to UPDATE ... FROM.");
                }
            }
        }

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (isUpdateFrom)
        {
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_33, "UPDATE FROM");
        }
#endif

        // JOINs (for SELECT/DELETE). For UPDATE FROM, joins go after SET.
        if (!isUpdateFrom)
        {
            for (int i = 0; i < q.Joins.Count; i++)
            {
                AppendSpacingNewline(sb, spacing, ref first);
                JoinInfo j = q.Joins[i];
                sb.Append(j.JoinType);
                sb.Append(' ');
                j.Sql.WriteSqlTo(sb);
                if (j.OnClause != null)
                {
                    sb.Append(" ON ");
                    j.OnClause.WriteSqlTo(sb);
                }
            }
        }

        // SET
        if (QueryType == QueryType.Update)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("SET ");
            for (int i = 0; i < SetProperties!.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(IdentifierGuard.Quote(SetProperties[i].Name));
                sb.Append(" = ");
                SetProperties[i].Expression.WriteSqlTo(sb);
            }
        }

        // FROM (UPDATE FROM source tables)
        if (isUpdateFrom)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("FROM ");
            for (int i = 0; i < q.Joins.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                q.Joins[i].Sql.WriteSqlTo(sb);
            }
        }

        // WHERE (includes UPDATE FROM join ON clauses)
        List<SQLiteExpression> wheres = q.Wheres.ToList();
        if (isUpdateFrom)
        {
            int insertAt = 0;
            foreach (JoinInfo join in q.Joins)
            {
                if (join.OnClause != null)
                {
                    wheres.Insert(insertAt++, join.OnClause);
                }
            }
        }

        bool allPredicateInWhere = q.AllPredicate != null && q.GroupBys.Count == 0;

        if (wheres.Count > 0 || allPredicateInWhere)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("WHERE ");
            int termCount = wheres.Count + (allPredicateInWhere ? 1 : 0);
            bool needAnd = false;
            for (int i = 0; i < wheres.Count; i++)
            {
                if (needAnd) sb.Append(" AND ");
                SQLiteExpression where = termCount > 1 ? ExpressionHelpers.BracketIfNeeded(wheres[i]) : wheres[i];
                where.WriteSqlTo(sb);
                needAnd = true;
            }

            if (allPredicateInWhere)
            {
                if (needAnd) sb.Append(" AND ");
                sb.Append('(');
                q.AllPredicate!.WriteSqlTo(sb);
                sb.Append(") IS NOT 1");
            }
        }

        // GROUP BY
        if (q.GroupBys.Count > 0)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("GROUP BY ");
            for (int i = 0; i < q.GroupBys.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                q.GroupBys[i].WriteSqlTo(sb);
            }
        }

        // HAVING
        bool allPredicateInHaving = q.AllPredicate != null && q.GroupBys.Count > 0;

        if (q.Havings.Count > 0 || allPredicateInHaving)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("HAVING ");
            int havingCount = q.Havings.Count + (allPredicateInHaving ? 1 : 0);
            bool needAnd = false;
            for (int i = 0; i < q.Havings.Count; i++)
            {
                if (needAnd) sb.Append(" AND ");
                SQLiteExpression having = havingCount > 1 ? ExpressionHelpers.BracketIfNeeded(q.Havings[i]) : q.Havings[i];
                having.WriteSqlTo(sb);
                needAnd = true;
            }

            if (allPredicateInHaving)
            {
                if (needAnd) sb.Append(" AND ");
                sb.Append('(');
                q.AllPredicate!.WriteSqlTo(sb);
                sb.Append(") IS NOT 1");
            }
        }

        // Set operations come after the main body but before ORDER BY/LIMIT/OFFSET
        if (hasSetOperations)
        {
            for (int i = 0; i < q.SetOperations.Count; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(spacing);
                sb.Append(q.SetOperations[i].Type);
                sb.Append(Environment.NewLine);
                sb.Append(spacing);
                q.SetOperations[i].Sql.WriteSqlTo(sb);
            }
        }

        // ORDER BY
        if (q.OrderBys.Count > 0 && !useExists && QueryType == QueryType.Select)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("ORDER BY ");
            for (int i = 0; i < q.OrderBys.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                q.OrderBys[i].WriteSqlTo(sb);
            }
        }

        // LIMIT
        if (q.Take != null)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("LIMIT ");
            sb.Append(q.Take);
        }
        else if (q.Skip != null)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("LIMIT -1");
        }

        // OFFSET
        if (q.Skip != null)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("OFFSET ");
            sb.Append(q.Skip);
        }

        if (EmitReturning)
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "RETURNING");
#endif
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("RETURNING ");
            for (int i = 0; i < q.Selects.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                q.Selects[i].WriteSqlTo(sb);
                sb.Append(" AS \"");
                sb.Append(q.Selects[i].IdentifierText);
                sb.Append('"');
            }
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "We are checking the Queryable class")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "Entity types are preserved by user-rooted IQueryable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "We are calling the Queryable.Select method on user-rooted IQueryable<T>.")]
    private Expression? TranslateMethodExpression(MethodCallExpression mce)
    {
        Type? declaringType = mce.Method.DeclaringType;

        if (declaringType != typeof(Queryable) && declaringType != typeof(QueryableExtensions) && declaringType != typeof(SQLiteDatabase))
        {
            throw new NotSupportedException($"Unsupported method: {mce.Method}");
        }

        List<MethodCallExpression> methodCalls = [];
        MethodCallExpression callExpression = mce;

        while (true)
        {
            if (callExpression.Method.Name == nameof(QueryableExtensions.UseReflectionMaterializer)
                && callExpression.Method.DeclaringType == typeof(QueryableExtensions))
            {
                skipGeneratedMaterializers = true;
                if (callExpression.Arguments.Count > 0
                    && callExpression.Arguments[0] is MethodCallExpression innerMethod)
                {
                    callExpression = innerMethod;
                    continue;
                }

                break;
            }

            if (TryDesugarGroupByResultSelector(callExpression, out MethodCallExpression groupByCall, out MethodCallExpression selectCall))
            {
                methodCalls.Add(selectCall);
                callExpression = groupByCall;
            }

            methodCalls.Add(callExpression);
            if (callExpression.Arguments.Count == 0)
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

        bool[] isWindowSelect = new bool[methodCalls.Count];
        for (int i = 0; i < methodCalls.Count; i++)
        {
            MethodCallExpression candidate = methodCalls[i];
            if (candidate.Method.Name == nameof(Queryable.Select)
                && WindowCallDetector.Contains(((LambdaExpression)ExpressionHelpers.StripQuotes(candidate.Arguments[1])).Body))
            {
                isWindowSelect[i] = true;
            }
        }

        int wrapIdx = FindSubqueryBoundary(methodCalls, isWindowSelect);
        bool wrappedAsSubquery = false;

        if (wrapIdx >= 0)
        {
            Expression innerExpr = methodCalls[wrapIdx].Arguments[0];
            SQLTranslator innerTranslator = Visitor.CloneDeeper(level + 1);
            SQLQuery innerQuery = innerTranslator.Translate(innerExpr);

            if (innerQuery.Reverse)
            {
                throw new NotSupportedException(
                    "Reverse() is not supported before an operator that needs a subquery, such as " +
                    "SelectMany, Join, Distinct, or GroupBy. Apply Reverse() last in the chain instead.");
            }

            Type entityType = innerExpr.Type.GetGenericArguments()[0];
            char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
            string alias = $"{aliasChar}{Visitor.Counters.NextTableIndex(aliasChar)}";

            SQLiteParameter[]? innerParams = innerQuery.Parameters.Count == 0 ? null : innerQuery.Parameters.ToArray();
            Visitor.From = SQLiteExpression.Leaf(entityType, -1, $"({Environment.NewLine}{innerQuery.Sql}{Environment.NewLine}) AS {alias}", innerParams);

            if (TypeHelpers.IsSimple(entityType, database.Options) && innerTranslator.Selects.Count == 1)
            {
                KeyValuePair<string, Expression> shape = innerTranslator.Visitor.TableColumns.First();
                string columnName = innerTranslator.Selects[0].IdentifierText;
                Visitor.TableColumns = new Dictionary<string, Expression>
                {
                    [shape.Key] = SQLiteExpression.Leaf(entityType, Visitor.Counters.NextIdentifier(), $"{alias}.\"{columnName}\"")
                };
            }
            else
            {
                Visitor.TableColumns = entityType.GetProperties()
                    .ToDictionary(p => p.Name, Expression (p) => SQLiteExpression.Leaf(p.PropertyType, Visitor.Counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(p.Name)}"));
            }

            methodCalls.RemoveRange(wrapIdx + 1, methodCalls.Count - (wrapIdx + 1));
            wrappedAsSubquery = true;
        }

        if (!wrappedAsSubquery && IsTerminalCountOverGroupBy(methodCalls))
        {
            MethodCallExpression countCall = methodCalls[0];
            Expression innerExpr = countCall.Arguments[0];
            Type groupingType = innerExpr.Type.GetGenericArguments()[0];

            if (countCall.Arguments.Count == 2)
            {
                innerExpr = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Where),
                    [groupingType],
                    innerExpr,
                    countCall.Arguments[1]);
            }

            ParameterExpression groupingParam = Expression.Parameter(groupingType, "g");
            LambdaExpression selector = Expression.Lambda(Expression.Constant(1), groupingParam);
            MethodCallExpression projectedInner = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                [groupingType, typeof(int)],
                innerExpr,
                selector);

            SQLTranslator innerTranslator = Visitor.CloneDeeper(level + 1);
            SQLQuery innerQuery = innerTranslator.Translate(projectedInner);

            char aliasChar = 'g';
            string alias = $"{aliasChar}{Visitor.Counters.NextTableIndex(aliasChar)}";

            SQLiteParameter[] innerParams = innerQuery.Parameters.ToArray();
            Visitor.From = SQLiteExpression.Leaf(typeof(int), -1, $"({Environment.NewLine}{innerQuery.Sql}{Environment.NewLine}) AS {alias}", innerParams);
            Visitor.TableColumns = new Dictionary<string, Expression>();

            methodCalls.RemoveRange(1, methodCalls.Count - 1);
            if (countCall.Arguments.Count == 2)
            {
                methodCalls[0] = countCall.Method.Name == nameof(Queryable.LongCount)
                    ? Expression.Call(typeof(Queryable), nameof(Queryable.LongCount), [typeof(int)], projectedInner)
                    : Expression.Call(typeof(Queryable), nameof(Queryable.Count), [typeof(int)], projectedInner);
            }
            wrappedAsSubquery = true;
        }

        if (!wrappedAsSubquery && IsTerminalScalarAggregateOverGroupBy(methodCalls, out string aggregateName))
        {
            MethodCallExpression aggregateCall = methodCalls[0];
            Expression projectedInner;
            Type scalarType;

            if (aggregateCall.Arguments.Count == 2)
            {
                Type groupingType = aggregateCall.Arguments[0].Type.GetGenericArguments()[0];
                scalarType = ((LambdaExpression)ExpressionHelpers.StripQuotes(aggregateCall.Arguments[1])).Body.Type;
                projectedInner = Expression.Call(typeof(Queryable), nameof(Queryable.Select), [groupingType, scalarType], aggregateCall.Arguments[0], aggregateCall.Arguments[1]);
            }
            else
            {
                projectedInner = aggregateCall.Arguments[0];
                scalarType = projectedInner.Type.GetGenericArguments()[0];
            }

            SQLTranslator innerTranslator = Visitor.CloneDeeper(level + 1);
            SQLQuery innerQuery = innerTranslator.Translate(projectedInner);

            char aliasChar = 'g';
            string alias = $"{aliasChar}{Visitor.Counters.NextTableIndex(aliasChar)}";

            SQLiteParameter[]? innerParams = innerQuery.Parameters.Count == 0 ? null : innerQuery.Parameters.ToArray();
            Visitor.From = SQLiteExpression.Leaf(scalarType, -1, $"({Environment.NewLine}{innerQuery.Sql}{Environment.NewLine}) AS {alias}", innerParams);

            string columnName = innerTranslator.Selects[0].IdentifierText;
            SQLiteExpression columnExpr = SQLiteExpression.Leaf(scalarType, Visitor.Counters.NextIdentifier(), $"{alias}.\"{columnName}\"");
            Visitor.TableColumns = new Dictionary<string, Expression> { [string.Empty] = columnExpr };
            queryableMethodVisitor.Selects.Clear();
            queryableMethodVisitor.Selects.Add(columnExpr);

            methodCalls.RemoveRange(1, methodCalls.Count - 1);
            methodCalls[0] = aggregateName is nameof(Queryable.Max) or nameof(Queryable.Min)
                ? Expression.Call(typeof(Queryable), aggregateName, [scalarType], projectedInner)
                : Expression.Call(typeof(Queryable), aggregateName, Type.EmptyTypes, projectedInner);
            wrappedAsSubquery = true;
        }

        if (!wrappedAsSubquery)
        {
            if (callExpression.Method.ReturnType.IsAssignableTo(typeof(BaseSQLiteTable)))
            {
                object? obj = callExpression.Object != null
                    ? ExpressionHelpers.GetConstantValue(callExpression.Object)
                    : null;
                object?[]? args = callExpression.Arguments.Count == 0
                    ? null
                    : callExpression.Arguments.Select(ExpressionHelpers.GetConstantValue).ToArray();
                BaseSQLiteTable resultTable = (BaseSQLiteTable)callExpression.Method.Invoke(obj, args)!;

                Visitor.AssignTable(resultTable);
                methodCalls.RemoveAt(methodCalls.Count - 1);
            }
            else if (callExpression.Arguments.Count == 0)
            {
                throw new NotSupportedException($"Unsupported method: {callExpression.Method}");
            }
            else
            {
                Visitor.Visit(callExpression.Arguments[0]);
            }
        }

        if (methodCalls.Count == 0 || methodCalls.All(f => !IsSelectMethod(f.Method)))
        {
            Type selectType;

            if (methodCalls.Count > 0)
            {
                selectType = methodCalls[0].Type.IsAssignableTo(typeof(IQueryable))
                    ? methodCalls[0].Type.GetGenericArguments()[0]
                    : methodCalls[0].Type;
            }
            else
            {
                selectType = Visitor.From!.Type;
            }

            MethodCallExpression selectMethod = CreateIdentitySelectExpression(selectType);
            methodCalls.Insert(0, selectMethod);
        }

        Expression? selectExpression = null;

        for (int i = methodCalls.Count - 1; i >= 0; i--)
        {
            MethodCallExpression node = methodCalls[i];

            UnaryExpression? unaryExpression = node.Arguments
                .Skip(1)
                .OfType<UnaryExpression>()
                .FirstOrDefault(u => u.Operand is LambdaExpression);

            if (unaryExpression != null)
            {
                LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;
                Visitor.MethodArguments[lambdaExpression.Parameters[0]] = Visitor.TableColumns;
            }

            Expression expression = queryableMethodVisitor.Visit(node);

            if (node.Method.Name == nameof(Queryable.Select))
            {
                selectExpression = expression;
            }
        }

        return selectExpression;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "We are checking the Queryable class")]
    private Expression TranslateOtherExpression(Expression expression)
    {
        Type genericType;

        if (expression.Type.IsAssignableTo(typeof(BaseSQLiteTable)) && ExpressionHelpers.IsConstant(expression))
        {
            BaseSQLiteTable table = (BaseSQLiteTable)ExpressionHelpers.GetConstantValue(expression)!;
            genericType = table.ElementType;
        }
        else
        {
            genericType = expression.Type.GetInterfaces()
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>))
                ?.GenericTypeArguments[0] ?? throw new InvalidOperationException("Expression is not an IQueryable.");
        }

        MethodCallExpression selectMethod = CreateIdentitySelectExpression(genericType);

        UnaryExpression unaryExpression = selectMethod.Arguments
            .Skip(1)
            .OfType<UnaryExpression>()
            .First();
        LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;

        Visitor.Visit(expression);

        Visitor.MethodArguments[lambdaExpression.Parameters[0]] = Visitor.TableColumns;

        queryableMethodVisitor.Visit(selectMethod);

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(selectMethod.Arguments[1]);
        return lambda.Body;
    }

    private static void AppendSpacingNewline(StringBuilder sb, string spacing, ref bool first)
    {
        if (first)
        {
            sb.Append(spacing);
            first = false;
        }
        else
        {
            sb.Append(Environment.NewLine);
            sb.Append(spacing);
        }
    }

    private static bool IsTerminalCountOverGroupBy(List<MethodCallExpression> methodCalls)
    {
        if (methodCalls.Count < 2)
        {
            return false;
        }

        MethodCallExpression outer = methodCalls[0];
        string outerName = outer.Method.Name;
        if (outerName != nameof(Queryable.Count) && outerName != nameof(Queryable.LongCount))
        {
            return false;
        }

        for (int i = 1; i < methodCalls.Count; i++)
        {
            if (methodCalls[i].Method.Name == nameof(Queryable.GroupBy))
            {
                return true;
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Builds an expression tree for the translator.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Builds an expression tree for the translator.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "IGrouping<,>.Key is rooted by the framework.")]
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Builds an expression tree, not compiled.")]
    private static bool TryDesugarGroupByResultSelector(MethodCallExpression call, out MethodCallExpression groupByCall, out MethodCallExpression selectCall)
    {
        groupByCall = null!;
        selectCall = null!;

        if (call.Method.Name != nameof(Queryable.GroupBy) || call.Method.DeclaringType != typeof(Queryable))
        {
            return false;
        }

        Expression source = call.Arguments[0];
        Expression keySelectorArg = call.Arguments[1];
        LambdaExpression keySelector = (LambdaExpression)ExpressionHelpers.StripQuotes(keySelectorArg);

        Type tSource = keySelector.Parameters[0].Type;
        Type tKey = keySelector.Body.Type;
        Type tElement;
        LambdaExpression resultSelector;
        Expression? elementSelectorArg = null;

        if (call.Arguments.Count == 3)
        {
            if (ExpressionHelpers.StripQuotes(call.Arguments[2]) is not LambdaExpression second || second.Parameters.Count != 2)
            {
                return false;
            }

            resultSelector = second;
            tElement = tSource;
        }
        else if (call.Arguments.Count == 4)
        {
            LambdaExpression elementSelector = (LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[2]);
            LambdaExpression resultLambda = (LambdaExpression)ExpressionHelpers.StripQuotes(call.Arguments[3]);

            elementSelectorArg = call.Arguments[2];
            tElement = elementSelector.Body.Type;
            resultSelector = resultLambda;
        }
        else
        {
            return false;
        }

        groupByCall = elementSelectorArg == null
            ? Expression.Call(typeof(Queryable), nameof(Queryable.GroupBy), [tSource, tKey], source, keySelectorArg)
            : Expression.Call(typeof(Queryable), nameof(Queryable.GroupBy), [tSource, tKey, tElement], source, keySelectorArg, elementSelectorArg);

        Type groupingType = typeof(IGrouping<,>).MakeGenericType(tKey, tElement);
        ParameterExpression grp = Expression.Parameter(groupingType, "grp");
        Expression keyAccess = Expression.Property(grp, nameof(IGrouping<,>.Key));

        Expression rewrittenBody = new ParameterSubstitutor(resultSelector.Parameters[0], keyAccess).Visit(resultSelector.Body);
        rewrittenBody = new ParameterSubstitutor(resultSelector.Parameters[1], grp).Visit(rewrittenBody);

        LambdaExpression selectLambda = Expression.Lambda(rewrittenBody, grp);
        selectCall = Expression.Call(typeof(Queryable), nameof(Queryable.Select), [groupingType, resultSelector.Body.Type], groupByCall, Expression.Quote(selectLambda));

        return true;
    }

    private static bool IsTerminalScalarAggregateOverGroupBy(List<MethodCallExpression> methodCalls, out string aggregateName)
    {
        aggregateName = string.Empty;

        if (methodCalls.Count < 2)
        {
            return false;
        }

        string name = methodCalls[0].Method.Name;
        if (name is not (nameof(Queryable.Sum) or nameof(Queryable.Max) or nameof(Queryable.Min) or nameof(Queryable.Average)))
        {
            return false;
        }

        for (int i = 1; i < methodCalls.Count; i++)
        {
            if (methodCalls[i].Method.Name == nameof(Queryable.GroupBy))
            {
                aggregateName = name;
                return true;
            }
        }

        return false;
    }

    private static int FindSubqueryBoundary(List<MethodCallExpression> methodCalls, bool[] isWindowSelect)
    {
        QueryLevelParts level = QueryLevelParts.None;
        int boundary = -1;

        for (int i = methodCalls.Count - 1; i >= 0; i--)
        {
            string name = methodCalls[i].Method.Name;
            bool windowSelect = isWindowSelect[i];
            bool hasPredicate = methodCalls[i].Arguments.Count > 1;

            if (ConflictsWithLevel(name, level, windowSelect, hasPredicate))
            {
                boundary = i;
                level = QueryLevelParts.None;
            }

            level |= MethodParts(name, windowSelect);
        }

        return boundary;
    }

    private static bool ConflictsWithLevel(string name, QueryLevelParts level, bool windowSelect, bool hasPredicate)
    {
        QueryLevelParts blockedBy = name switch
        {
            nameof(Queryable.Where) => QueryLevelParts.Limit | QueryLevelParts.Window,
            nameof(Queryable.Any) or nameof(Queryable.All) or nameof(Queryable.Contains) => QueryLevelParts.Limit | QueryLevelParts.Window,
            nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault)
                when hasPredicate => QueryLevelParts.Limit | QueryLevelParts.Window,
            nameof(Queryable.Count) or nameof(Queryable.LongCount) or nameof(Queryable.Sum)
                or nameof(Queryable.Max) or nameof(Queryable.Min) or nameof(Queryable.Average) => QueryLevelParts.Window | QueryLevelParts.Limit,
            nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) => QueryLevelParts.Limit,
            nameof(Queryable.Distinct) => QueryLevelParts.Limit,
            nameof(Queryable.Select) when windowSelect => QueryLevelParts.Distinct | QueryLevelParts.Limit,
            nameof(Queryable.Select) => QueryLevelParts.Distinct,
            nameof(Queryable.GroupBy) => QueryLevelParts.Limit | QueryLevelParts.Distinct,
            _ when IsJoinLikeMethod(name) => QueryLevelParts.Where | QueryLevelParts.Projection
                | QueryLevelParts.GroupBy | QueryLevelParts.Limit | QueryLevelParts.Distinct | QueryLevelParts.Reverse,
            _ => QueryLevelParts.None
        };

        return (level & blockedBy) != QueryLevelParts.None;
    }

    private static QueryLevelParts MethodParts(string name, bool windowSelect)
    {
        QueryLevelParts parts = name switch
        {
            nameof(Queryable.Where) => QueryLevelParts.Where,
            nameof(Queryable.Select) => QueryLevelParts.Projection,
            nameof(Queryable.GroupBy) => QueryLevelParts.GroupBy,
            nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) => QueryLevelParts.OrderBy,
            nameof(Queryable.Distinct) => QueryLevelParts.Distinct,
            nameof(Queryable.Take) or nameof(Queryable.Skip)
                or nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault)
                or nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault) => QueryLevelParts.Limit,
            nameof(Queryable.Reverse) => QueryLevelParts.Reverse,
            _ when IsJoinLikeMethod(name) => QueryLevelParts.Join,
            _ => QueryLevelParts.None
        };

        if (windowSelect)
        {
            parts |= QueryLevelParts.Window;
        }

        return parts;
    }

    private static bool IsJoinLikeMethod(string name)
    {
        return name == nameof(Queryable.SelectMany)
            || name == nameof(Queryable.Join)
#if NET10_0_OR_GREATER
            || name == nameof(Queryable.LeftJoin)
            || name == nameof(Queryable.RightJoin)
#endif
            || name == nameof(QueryableExtensions.FullOuterJoin)
            || name == nameof(Queryable.GroupJoin);
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
            [genericType, genericType],
            sourceParameter,
            selector
        );
    }

    private static bool IsSelectMethod(MethodInfo method)
    {
        return TranslationPatterns.IsSelectMethodName(method.Name);
    }

    private static bool UsesEntityMaterializer(Expression? selectMethodExpression)
    {
        if (selectMethodExpression is null or ParameterExpression)
        {
            return true;
        }

        if (selectMethodExpression is MemberExpression { Expression: not SQLiteExpression } memberSelect)
        {
            return IsRawColumnPassthroughMember(memberSelect);
        }

        if (selectMethodExpression is MethodCallExpression mce)
        {
            return mce.Method.DeclaringType == typeof(Queryable)
                   || mce.Method.DeclaringType == typeof(Enumerable);
        }

        return false;
    }

    private static bool IsRawColumnPassthroughMember(MemberExpression member)
    {
        Expression? current = member.Expression;
        while (current is MemberExpression inner)
        {
            current = inner.Expression;
        }

        return current is not (NewExpression or MemberInitExpression or MethodCallExpression);
    }
}
