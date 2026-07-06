namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private readonly SQLVisitor visitor;
    private readonly SQLiteOptions options;

    private readonly List<string> wheres = [];
    private readonly List<string> havings = [];
    private readonly List<string> orderBys = [];
    private readonly List<string> groupBys = [];
    private readonly List<SQLiteParameter> parameters = [];
    private string selectExpr = "\"value\"";
    private string keyColumn = "\"key\"";
    private string? groupKeySql;
    private string? groupElementSql;
    private Type currentElementType = typeof(object);
    private string baseSource = "";
    private string baseAlias = "";
    private string? fromOverride;
    private string? limit;
    private string? offset;
    private bool distinct;
    private bool reverseApplied;
    private bool distinctSeenReverse;
    private bool wrapInArray = true;
    private bool singleSemantic;
    private bool countsGroups;
    private string? existsWrapper;
    private string? crossJoin;

    public JsonCollectionVisitor(SQLVisitor visitor, SQLiteOptions options)
    {
        this.visitor = visitor;
        this.options = options;
    }

    private string VisitLambda(Expression arg, Type elementType, bool coalesceLiftedComparison = false)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, selectExpr);

        string sql = TranslateBody(lambda.Body, coalesceLiftedComparison);
        visitor.MethodArguments.Remove(param);
        return sql;
    }

    private string VisitLambdaAliased(Expression arg, Type elementType, string alias)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, $"{alias}.\"value\"");

        string sql = TranslateBody(lambda.Body);
        visitor.MethodArguments.Remove(param);
        return sql;
    }

    private string TranslateBody(Expression body, bool coalesceLiftedComparison = false)
    {
        Expression result = visitor.Visit(body);

        if (result is not SQLiteExpression sqlExpr)
        {
            throw new NotSupportedException($"Cannot translate lambda body: {body}");
        }

        if (coalesceLiftedComparison)
        {
            sqlExpr = visitor.CoalesceLiftedOrderComparison(body, sqlExpr);
        }

        if (sqlExpr.Parameters != null)
        {
            parameters.AddRange(sqlExpr.Parameters);
        }

        return sqlExpr.ToString();
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void BindParameter(ParameterExpression param, Type elementType, string valueSql)
    {
        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>) && groupKeySql != null)
        {
            Type keyType = elementType.GetGenericArguments()[0];
            Type groupElementType = elementType.GetGenericArguments()[1];
            visitor.MethodArguments[param] = new Dictionary<string, Expression>
            {
                [nameof(IGrouping<,>.Key)] = SQLiteExpression.Leaf(keyType, -1, groupKeySql, null).WithJsonSource(),
                [string.Empty] = SQLiteExpression.Leaf(groupElementType, -1, groupElementSql!, null).WithJsonSource()
            };
            return;
        }

        if (TypeHelpers.IsSimple(elementType, options))
        {
            SQLiteExpression valueExpr = SQLiteExpression.Leaf(elementType, -1, valueSql, null).WithJsonSource();
            visitor.MethodArguments[param] = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            Dictionary<string, Expression> dict = new();
            RegisterProperties(elementType, string.Empty, string.Empty, valueSql, dict);
            visitor.MethodArguments[param] = dict;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void RegisterProperties(Type type, string prefix, string jsonPrefix, string valueSql, Dictionary<string, Expression> dict)
    {
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            bool atRoot = string.IsNullOrEmpty(prefix);
            string dictKey = atRoot ? prop.Name : $"{prefix}.{prop.Name}";
            string jsonName = CommonHelpers.JsonPathSegment(CommonHelpers.JsonMemberName(type, prop, options));
            string jsonKey = atRoot ? jsonName : $"{jsonPrefix}.{jsonName}";

            if (TypeHelpers.IsSimple(prop.PropertyType, options))
            {
                string sql = $"json_extract({valueSql}, {CommonHelpers.JsonExtractPathLiteral(jsonKey)})";
                dict[dictKey] = SQLiteExpression.Leaf(prop.PropertyType, -1, sql, null).WithJsonSource();
            }
            else
            {
                RegisterProperties(prop.PropertyType, dictKey, jsonKey, valueSql, dict);
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
        baseAlias = $"j{visitor.Counters.NextTableIndex('j')}";
        selectExpr = $"{baseAlias}.\"value\"";
        keyColumn = $"{baseAlias}.\"key\"";
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
