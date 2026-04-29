namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private static readonly HashSet<string> CollectionMethods =
    [
        nameof(Enumerable.Where),
        nameof(Enumerable.Select),
        nameof(Enumerable.OrderBy),
        nameof(Enumerable.OrderByDescending),
        nameof(Enumerable.ThenBy),
        nameof(Enumerable.ThenByDescending),
        nameof(Enumerable.GroupBy),
        nameof(Enumerable.SelectMany),
        nameof(Enumerable.Skip),
        nameof(Enumerable.Take),
        nameof(Enumerable.First),
        nameof(Enumerable.FirstOrDefault),
        nameof(Enumerable.Last),
        nameof(Enumerable.LastOrDefault),
        nameof(Enumerable.Single),
        nameof(Enumerable.SingleOrDefault),
        nameof(Enumerable.Count),
        nameof(Enumerable.Any),
        nameof(Enumerable.All),
        nameof(Enumerable.Min),
        nameof(Enumerable.Max),
        nameof(Enumerable.Sum),
        nameof(Enumerable.Average),
        nameof(Enumerable.Distinct),
        nameof(Enumerable.Reverse),
        nameof(Enumerable.ElementAt),
        nameof(Enumerable.Contains)
    ];

    private readonly SQLVisitor visitor;
    private readonly SQLiteOptions options;

    private readonly List<string> wheres = [];
    private readonly List<string> orderBys = [];
    private readonly List<string> groupBys = [];
    private readonly List<SQLiteParameter> parameters = [];
    private string selectExpr = "value";
    private string? limit;
    private string? offset;
    private bool distinct;
    private bool wrapInArray = true;
    private bool singleSemantic;
    private string? existsWrapper;
    private string? crossJoin;

    private JsonCollectionVisitor(SQLVisitor visitor, SQLiteOptions options)
    {
        this.visitor = visitor;
        this.options = options;
    }

    public static Expression? TryHandle(MethodCallExpression node, SQLVisitor sqlVisitor)
    {
        if (!IsChainedCollectionMethod(node))
        {
            return null;
        }

        List<MethodCallExpression> chain = [];
        Expression source = UnwindChain(node, chain);

        ResolvedModel sourceModel = sqlVisitor.ResolveExpression(source);
        if (sourceModel.SQLiteExpression == null)
        {
            return null;
        }

        if (!IsJsonCollection(sourceModel.SQLiteExpression.Type, sqlVisitor.Database.Options))
        {
            return null;
        }

        JsonCollectionVisitor jcv = new(sqlVisitor, sqlVisitor.Database.Options);
        jcv.parameters.AddRange(sourceModel.SQLiteExpression.Parameters ?? []);

        Type resultType = node.Type;
        foreach (MethodCallExpression call in chain)
        {
            jcv.ProcessMethod(call, sourceModel.SQLiteExpression.Type);
            resultType = call.Type;
        }

        string sql = jcv.BuildSql(sourceModel.SQLiteExpression.Sql);
        Type coercedType = CoerceType(resultType, sourceModel.SQLiteExpression.Type);
        return new SQLiteExpression(coercedType, sqlVisitor.Counters.IdentifierIndex++, sql,
            jcv.parameters.Count > 0 ? jcv.parameters.ToArray() : null)
        {
            IsJsonSource = true,
        };
    }

    private string VisitLambda(Expression arg, Type elementType)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, "value");

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLiteExpression sqlExpr)
        {
            if (sqlExpr.Parameters != null)
            {
                parameters.AddRange(sqlExpr.Parameters);
            }

            return sqlExpr.Sql;
        }

        throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
    }

    private string VisitLambdaAliased(Expression arg, Type elementType, string alias)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, $"{alias}.value");

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLiteExpression sqlExpr)
        {
            if (sqlExpr.Parameters != null)
            {
                parameters.AddRange(sqlExpr.Parameters);
            }

            return sqlExpr.Sql;
        }

        throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void BindParameter(ParameterExpression param, Type elementType, string valueSql)
    {
        if (TypeHelpers.IsSimple(elementType, options))
        {
            SQLiteExpression valueExpr = new(elementType, -1, valueSql, (SQLiteParameter[]?)null);
            visitor.MethodArguments[param] = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            Dictionary<string, Expression> dict = new();
            RegisterProperties(elementType, string.Empty, valueSql, dict);
            visitor.MethodArguments[param] = dict;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void RegisterProperties(Type type, string prefix, string valueSql, Dictionary<string, Expression> dict)
    {
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string dictKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            if (TypeHelpers.IsSimple(prop.PropertyType, options))
            {
                string sql = $"json_extract({valueSql}, '$.{dictKey}')";
                dict[dictKey] = new SQLiteExpression(prop.PropertyType, -1, sql, (SQLiteParameter[]?)null);
            }
            else
            {
                RegisterProperties(prop.PropertyType, dictKey, valueSql, dict);
            }
        }
    }

    private void AddParameters(ResolvedModel model)
    {
        if (model.SQLiteExpression?.Parameters != null)
        {
            parameters.AddRange(model.SQLiteExpression.Parameters);
        }
    }

    private static bool IsChainedCollectionMethod(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Enumerable) || !CollectionMethods.Contains(node.Method.Name))
        {
            return false;
        }

        bool hasInnerChainCall = node.Arguments.Count > 0
            && node.Arguments[0] is MethodCallExpression innerCall
            && innerCall.Method.DeclaringType == typeof(Enumerable)
            && CollectionMethods.Contains(innerCall.Method.Name);
        bool takesPredicate = node.Arguments.Count >= 2;

        return hasInnerChainCall || takesPredicate
            || node.Method.Name is nameof(Enumerable.ThenBy) or nameof(Enumerable.ThenByDescending)
            or nameof(Enumerable.GroupBy) or nameof(Enumerable.SelectMany);
    }

    private static Expression UnwindChain(MethodCallExpression node, List<MethodCallExpression> chain)
    {
        Expression current = node;
        while (current is MethodCallExpression call
               && call.Method.DeclaringType == typeof(Enumerable)
               && CollectionMethods.Contains(call.Method.Name))
        {
            chain.Insert(0, call);
            current = call.Arguments[0];
        }

        return current;
    }

    private static bool IsJsonCollection(Type type, SQLiteOptions options)
    {
        return options.TypeConverters.ContainsKey(type)
               && TypeHelpers.GetEnumerableElementType(type) != null;
    }

    private static Type CoerceType(Type declaredType, Type sourceType)
    {
        if (declaredType.IsAssignableFrom(sourceType))
        {
            return sourceType;
        }

        if (TypeHelpers.GetEnumerableElementType(declaredType) is Type declaredElem
            && TypeHelpers.GetEnumerableElementType(sourceType) is Type sourceElem
            && declaredElem == sourceElem)
        {
            return sourceType;
        }

        return declaredType;
    }
}
