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

    public QueryType QueryType { get; init; }

    public List<(string Name, SQLiteExpression Expression)>? SetProperties { get; set; }

    public void Visit(Expression node)
    {
        node = QueryFilterInjector.Inject(node, Visitor.Database.Options);
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

        // Visit all expressions to collect parameters before composing SQL.
        if (QueryType == QueryType.Select && !useExists)
        {
            foreach (SQLiteExpression expression in queryableMethodVisitor.Selects)
            {
                VisitSQLExpression(expression);
            }
        }

        VisitSQLExpression(Visitor.From);

        if (QueryType == QueryType.Update && SetProperties != null)
        {
            foreach ((string _, SQLiteExpression sqlExpression) in SetProperties)
            {
                VisitSQLExpression(sqlExpression);
            }
        }

        foreach (SQLiteExpression sqlExpression in queryableMethodVisitor.Wheres)
        {
            VisitSQLExpression(sqlExpression);
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

        bool hasSetOperations = queryableMethodVisitor.SetOperations.Count > 0;

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

        if (level == 0 && cteRegistry?.Ctes.Count > 0)
        {
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
                cteSb.Append(" AS (");
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

        if (hasReflectedArg
            && rawSignature2 != null
            && selectMaterializers2.Count > 0
            && selectMaterializers2.TryGetValue(rawSignature2, out Func<SQLiteQueryContext, object?>? generatedAnon))
        {
#if SQLITE_FRAMEWORK_TESTING
            database.IncrementSelectMaterializerHits();
#endif
            createObject = generatedAnon;
            if (selectMethodExpression != null)
            {
                ReflectedBindingsCollector anonCollector = new();
                anonCollector.Visit(queryableMethodVisitor.LastSelectLambdaBody);
                if (anonCollector.Methods.Count > 0)
                {
                    reflectedMethods = anonCollector.Methods;
                    reflectedInstances = anonCollector.Instances;
                }
                if (anonCollector.CapturedValues.Count > 0)
                {
                    capturedValues = anonCollector.CapturedValues;
                }
                if (anonCollector.Types.Count > 0)
                {
                    reflectedTypes = anonCollector.Types;
                }
                if (anonCollector.Members.Count > 0)
                {
                    reflectedMembers = anonCollector.Members;
                }
                if (anonCollector.Constructors.Count > 0)
                {
                    reflectedConstructors = anonCollector.Constructors;
                }
            }
        }
        else if (selectMethodExpression is null
            or ParameterExpression
            or MemberExpression { Expression: not SQLiteExpression }
            || (selectMethodExpression is MethodCallExpression mce
                && (mce.Method.DeclaringType == typeof(Queryable)
                    || mce.Method.DeclaringType == typeof(Enumerable))))
        {
            createObject = null;
        }
        else
        {
            IReadOnlyDictionary<string, Func<SQLiteQueryContext, object?>> selectMaterializers = database.Options.SelectMaterializers;
            string? rawSignature = queryableMethodVisitor.RawSelectSignature;
            if (selectMaterializers.Count > 0 && rawSignature != null
                && selectMaterializers.TryGetValue(rawSignature, out Func<SQLiteQueryContext, object?>? generated))
            {
#if SQLITE_FRAMEWORK_TESTING
                database.IncrementSelectMaterializerHits();
#endif
                createObject = generated;

                ReflectedBindingsCollector collector = new();
                collector.Visit(selectMethodExpression);
                if (collector.Methods.Count > 0)
                {
                    reflectedMethods = collector.Methods;
                    reflectedInstances = collector.Instances;
                }
                if (collector.CapturedValues.Count > 0)
                {
                    capturedValues = collector.CapturedValues;
                }
                if (collector.Types.Count > 0)
                {
                    reflectedTypes = collector.Types;
                }
                if (collector.Members.Count > 0)
                {
                    reflectedMembers = collector.Members;
                }
                if (collector.Constructors.Count > 0)
                {
                    reflectedConstructors = collector.Constructors;
                }
            }
            else
            {
                if (rawSignature != null && database.Options.ReflectionFallbackDisabled)
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
                CompiledExpression compiledExpression = (CompiledExpression)compilerVisitor.Visit(selectMethodExpression);
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
            ThrowOnMoreThanOne = queryableMethodVisitor.ThrowOnMoreThanOne,
            DefaultValue = queryableMethodVisitor.DefaultValue,
            HasDefaultValue = queryableMethodVisitor.HasDefaultValue,
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

        // SELECT
        if (QueryType == QueryType.Select)
        {
            sb.Append(spacing);
            sb.Append("SELECT");
            if (q.IsDistinct) sb.Append(" DISTINCT");
            sb.Append(' ');
            if (useExists)
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

        // JOINs
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

        // SET
        if (QueryType == QueryType.Update && SetProperties != null)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("SET ");
            for (int i = 0; i < SetProperties.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(SetProperties[i].Name);
                sb.Append(" = ");
                SetProperties[i].Expression.WriteSqlTo(sb);
            }
        }

        // WHERE
        if (q.Wheres.Count > 0)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("WHERE ");
            if (q.IsAll)
            {
                sb.Append("NOT (");
                for (int i = 0; i < q.Wheres.Count; i++)
                {
                    if (i > 0) sb.Append(" AND ");
                    q.Wheres[i].WriteSqlTo(sb);
                }
                sb.Append(')');
            }
            else
            {
                for (int i = 0; i < q.Wheres.Count; i++)
                {
                    if (i > 0) sb.Append(" AND ");
                    q.Wheres[i].WriteSqlTo(sb);
                }
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
        if (q.Havings.Count > 0)
        {
            AppendSpacingNewline(sb, spacing, ref first);
            sb.Append("HAVING ");
            for (int i = 0; i < q.Havings.Count; i++)
            {
                if (i > 0) sb.Append(" AND ");
                q.Havings[i].WriteSqlTo(sb);
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
        if (q.OrderBys.Count > 0 && !useExists)
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

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "We are checking the Queryable class")]
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

        if (callExpression.Arguments.Count == 0)
        {
            if (callExpression.Method.ReturnType.IsAssignableTo(typeof(BaseSQLiteTable)))
            {
                object? obj = callExpression.Object != null
                    ? ExpressionHelpers.GetConstantValue(callExpression.Object)
                    : null;
                BaseSQLiteTable resultTable = (BaseSQLiteTable)callExpression.Method.Invoke(obj, null)!;

                Visitor.AssignTable(resultTable.ElementType);
                methodCalls.RemoveAt(methodCalls.Count - 1);
            }
            else
            {
                throw new NotSupportedException($"Unsupported method: {callExpression.Method}");
            }
        }
        else
        {
            Visitor.Visit(callExpression.Arguments[0]);
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
                .FirstOrDefault();

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
        return method.GetParameters().Length > 0 && method.Name is nameof(Queryable.Select)
            or nameof(Queryable.Min)
            or nameof(Queryable.Max)
            or nameof(Queryable.Sum)
            or nameof(Queryable.Count)
            or nameof(Queryable.LongCount)
            or nameof(Queryable.Average)
            or nameof(Queryable.Contains);
    }
}