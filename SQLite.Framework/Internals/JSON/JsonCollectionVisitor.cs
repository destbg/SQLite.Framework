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
    private readonly List<string> innerAliases = [];
    private string selectExpr = "\"value\"";
    private string keyColumn = "\"key\"";
    private string? groupKeySql;
    private string? groupElementSql;
    private bool groupWindowMaterialized;
    private Type currentElementType = typeof(object);
    private string baseSource = "";
    private string baseAlias = "";
    private string? fromOverride;
    private string? limit;
    private string? offset;
    private int primaryOrderCount;
    private bool distinct;
    private bool reverseApplied;
    private bool distinctSeenReverse;
    private bool wrapInArray = true;
    private bool singleSemantic;
    private bool countsGroups;
    private string? existsWrapper;
    private string? crossJoin;
    private Type? stringEnumNameWrapType;

    public JsonCollectionVisitor(SQLVisitor visitor, SQLiteOptions options)
    {
        this.visitor = visitor;
        this.options = options;
    }

    private string VisitLambda(Expression arg, Type elementType, bool coalesceLiftedComparison = false)
    {
        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        EnsureGroupAggregatesAvailable(lambda, elementType);
        BindParameter(param, elementType, selectExpr);

        string sql = TranslateBody(lambda.Body, coalesceLiftedComparison);
        visitor.MethodArguments.Remove(param);
        return sql;
    }

    private string TranslateBody(Expression body, bool coalesceLiftedComparison = false)
    {
        Expression result = JsonArrayLiteralTranslator.TryTranslate(visitor, body) ?? visitor.Visit(body);

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

    private void EnsureGroupAggregatesAvailable(LambdaExpression lambda, Type elementType)
    {
        if (!groupWindowMaterialized
            || !elementType.IsGenericType
            || elementType.GetGenericTypeDefinition() != typeof(IGrouping<,>))
        {
            return;
        }

        JsonGroupAggregateFinder finder = new(lambda.Parameters[0]);
        finder.Visit(lambda.Body);
        if (finder.Found)
        {
            throw new NotSupportedException(
                "A group aggregate after Take or Skip on a JSON grouping is not supported, because the paged groups no longer carry their elements.");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void BindParameter(ParameterExpression param, Type elementType, string valueSql)
    {
        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>) && groupKeySql != null)
        {
            Type keyType = elementType.GetGenericArguments()[0];
            Type groupElementType = elementType.GetGenericArguments()[1];
            Dictionary<string, Expression> groupColumns = new()
            {
                [nameof(IGrouping<,>.Key)] = SQLiteExpression.Leaf(keyType, -1, groupKeySql, null).WithJsonSource()
            };
            if (groupElementSql != null)
            {
                groupColumns[string.Empty] = SQLiteExpression.Leaf(groupElementType, -1, groupElementSql, null).WithJsonSource();
            }

            visitor.MethodArguments[param] = groupColumns;
            return;
        }

        if (TypeHelpers.IsSimple(elementType, options))
        {
            SQLiteExpression valueExpr = SQLiteExpression.Leaf(elementType, -1, valueSql, null).WithJsonSource();
            visitor.MethodArguments[param] = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            Dictionary<string, Expression> dict = new()
            {
                [string.Empty] = SQLiteExpression.Leaf(elementType, -1, valueSql, (SQLiteParameter[]?)null).WithJsonSource()
            };
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
        innerAliases.Clear();
        innerAliases.Add(baseAlias);
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
