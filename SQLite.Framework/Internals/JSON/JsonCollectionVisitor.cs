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

    public JsonCollectionVisitor(SQLVisitor visitor, SQLiteOptions options)
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

    public (string Sql, SQLiteParameter[]? Parameters, Type ResultType) Run(SQLiteExpression sourceExpr, List<MethodCallExpression> chain, Type resultType)
    {
        parameters.AddRange(sourceExpr.Parameters ?? []);

        currentElementType = TypeHelpers.GetEnumerableElementType(sourceExpr.Type)!;
        baseSource = sourceExpr.ToString();
        Type rt = resultType;
        foreach (MethodCallExpression call in chain)
        {
            ProcessMethod(call);
            rt = call.Type;
        }

        string sql = BuildSql(sourceExpr.ToString());
        return (sql, parameters.Count > 0 ? parameters.ToArray() : null, rt);
    }
}
