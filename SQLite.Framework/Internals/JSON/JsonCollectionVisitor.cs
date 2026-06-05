namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private readonly SQLVisitor visitor;
    private readonly SQLiteOptions options;

    private readonly List<string> wheres = [];
    private readonly List<string> orderBys = [];
    private readonly List<string> groupBys = [];
    private readonly List<SQLiteParameter> parameters = [];
    private string selectExpr = "\"value\"";
    private string keyColumn = "\"key\"";
    private Type currentElementType = typeof(object);
    private string baseSource = "";
    private string baseJoinSuffix = "";
    private string? fromOverride;
    private string? limit;
    private string? offset;
    private bool distinct;
    private bool reverseApplied;
    private bool distinctSeenReverse;
    private bool wrapInArray = true;
    private bool singleSemantic;
    private string? existsWrapper;
    private string? crossJoin;

    private JsonCollectionVisitor(SQLVisitor visitor, SQLiteOptions options)
    {
        this.visitor = visitor;
        this.options = options;
    }

    private string VisitLambda(Expression arg, Type elementType)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, selectExpr);

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLiteExpression sqlExpr)
        {
            if (sqlExpr.Parameters != null)
            {
                parameters.AddRange(sqlExpr.Parameters);
            }

            return sqlExpr.ToString();
        }

        throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
    }

    private string VisitLambdaAliased(Expression arg, Type elementType, string alias)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, $"{alias}.\"value\"");

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLiteExpression sqlExpr)
        {
            if (sqlExpr.Parameters != null)
            {
                parameters.AddRange(sqlExpr.Parameters);
            }

            return sqlExpr.ToString();
        }

        throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void BindParameter(ParameterExpression param, Type elementType, string valueSql)
    {
        if (TypeHelpers.IsSimple(elementType, options))
        {
            SQLiteExpression valueExpr = SQLiteExpression.Leaf(elementType, -1, valueSql, null);
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
                dict[dictKey] = SQLiteExpression.Leaf(prop.PropertyType, -1, sql, null);
            }
            else
            {
                RegisterProperties(prop.PropertyType, dictKey, valueSql, dict);
            }
        }
    }

    private void AddParameters(ResolvedModel model)
    {
        if (model.SQLiteExpression!.Parameters != null)
        {
            parameters.AddRange(model.SQLiteExpression.Parameters);
        }
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

        if (!IsJsonCollectionExpression(sourceModel.SQLiteExpression, sqlVisitor.Database.Options))
        {
            return null;
        }

        JsonCollectionVisitor jcv = new(sqlVisitor, sqlVisitor.Database.Options);
        jcv.parameters.AddRange(sourceModel.SQLiteExpression.Parameters ?? []);

        jcv.currentElementType = TypeHelpers.GetEnumerableElementType(sourceModel.SQLiteExpression.Type)!;
        jcv.baseSource = sourceModel.SQLiteExpression.ToString();
        Type resultType = node.Type;
        foreach (MethodCallExpression call in chain)
        {
            jcv.ProcessMethod(call);
            resultType = call.Type;
        }

        string sql = jcv.BuildSql(sourceModel.SQLiteExpression.ToString());
        Type coercedType = CoerceType(resultType, sourceModel.SQLiteExpression.Type);
        return SQLiteExpression.Leaf(coercedType, sqlVisitor.Counters.NextIdentifier(), sql,
            jcv.parameters.Count > 0 ? jcv.parameters.ToArray() : null)
            .WithJsonSource();
    }

    private static bool IsChainedCollectionMethod(MethodCallExpression node)
    {
        if (!TranslationPatterns.IsJsonCollectionMethod(node.Method.Name))
        {
            return false;
        }

        bool hasInnerChainCall = node.Arguments.Count > 0
            && node.Arguments[0] is MethodCallExpression innerCall
            && innerCall.Method.DeclaringType == typeof(Enumerable)
            && TranslationPatterns.IsJsonCollectionMethod(innerCall.Method.Name);
        bool takesPredicate = node.Arguments.Count >= 2;

        return hasInnerChainCall || takesPredicate;
    }

    private static Expression UnwindChain(MethodCallExpression node, List<MethodCallExpression> chain)
    {
        Expression current = node;
        while (current is MethodCallExpression call
               && call.Method.DeclaringType == typeof(Enumerable)
               && TranslationPatterns.IsJsonCollectionMethod(call.Method.Name))
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

    private static bool IsJsonCollectionExpression(SQLiteExpression expr, SQLiteOptions options)
    {
        return expr.IsJsonSource || IsJsonCollection(expr.Type, options);
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
