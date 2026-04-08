using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.JsonB;

internal class JsonCollectionVisitor
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
        nameof(Enumerable.Concat),
        nameof(Enumerable.Union),
        nameof(Enumerable.Intersect),
        nameof(Enumerable.Except),
        nameof(Enumerable.ElementAt),
        nameof(Enumerable.Contains)
    ];

    private readonly SQLVisitor visitor;
    private readonly SQLiteStorageOptions options;

    private readonly List<string> wheres = [];
    private readonly List<string> orderBys = [];
    private readonly List<string> groupBys = [];
    private readonly List<SQLiteParameter> parameters = [];
    private string selectExpr = "value";
    private string? limit;
    private string? offset;
    private bool distinct;
    private bool wrapInArray = true;
    private string? existsWrapper;
    private string? crossJoin;

    public JsonCollectionVisitor(SQLVisitor visitor, SQLiteStorageOptions options)
    {
        this.visitor = visitor;
        this.options = options;
    }

    public static Expression? TryHandle(MethodCallExpression node, ISQLExpressionVisitor expressionVisitor)
    {
        if (!IsChainedCollectionMethod(node))
        {
            return null;
        }

        SQLVisitor sqlVisitor = (SQLVisitor)expressionVisitor;

        List<MethodCallExpression> chain = [];
        Expression source = UnwindChain(node, chain);

        ResolvedModel sourceModel = sqlVisitor.ResolveExpression(source);
        if (sourceModel.SQLExpression == null)
        {
            return null;
        }

        if (!IsJsonCollection(sourceModel.SQLExpression.Type, sqlVisitor.Database.StorageOptions))
        {
            return null;
        }

        JsonCollectionVisitor jcv = new(sqlVisitor, sqlVisitor.Database.StorageOptions);
        if (sourceModel.SQLExpression.Parameters != null)
        {
            jcv.parameters.AddRange(sourceModel.SQLExpression.Parameters);
        }

        Type resultType = node.Type;
        foreach (MethodCallExpression call in chain)
        {
            jcv.ProcessMethod(call, sourceModel.SQLExpression.Type);
            resultType = call.Type;
        }

        string sql = jcv.BuildSql(sourceModel.SQLExpression.Sql);
        Type coercedType = CoerceType(resultType, sourceModel.SQLExpression.Type, sqlVisitor.Database.StorageOptions);
        return new SQLExpression(coercedType, sqlVisitor.IdentifierIndex.Index++, sql,
            jcv.parameters.Count > 0 ? jcv.parameters.ToArray() : null);
    }

    private static bool IsChainedCollectionMethod(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Enumerable))
        {
            return false;
        }

        if (!CollectionMethods.Contains(node.Method.Name))
        {
            return false;
        }

        if (node.Arguments.Count > 0
            && node.Arguments[0] is MethodCallExpression innerCall
            && innerCall.Method.DeclaringType == typeof(Enumerable)
            && CollectionMethods.Contains(innerCall.Method.Name))
        {
            return true;
        }

        if (node.Method.Name is nameof(Enumerable.ThenBy) or nameof(Enumerable.ThenByDescending)
            or nameof(Enumerable.GroupBy) or nameof(Enumerable.SelectMany))
        {
            return true;
        }

        return false;
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

    private static bool IsJsonCollection(Type type, SQLiteStorageOptions options)
    {
        return options.TypeConverters.ContainsKey(type)
               && CommonHelpers.GetEnumerableElementType(type) != null;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void ProcessMethod(MethodCallExpression call, Type sourceType)
    {
        Type elementType = CommonHelpers.GetEnumerableElementType(sourceType) ?? typeof(object);

        switch (call.Method.Name)
        {
            case nameof(Enumerable.Where):
            {
                string predSql = VisitLambda(call.Arguments[1], elementType);
                wheres.Add(predSql);
                break;
            }
            case nameof(Enumerable.OrderBy):
            {
                orderBys.Clear();
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} ASC");
                break;
            }
            case nameof(Enumerable.OrderByDescending):
            {
                orderBys.Clear();
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} DESC");
                break;
            }
            case nameof(Enumerable.ThenBy):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} ASC");
                break;
            }
            case nameof(Enumerable.ThenByDescending):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} DESC");
                break;
            }
            case nameof(Enumerable.GroupBy):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                groupBys.Add(keySql);
                break;
            }
            case nameof(Enumerable.Select):
            {
                string projSql = VisitLambda(call.Arguments[1], elementType);
                selectExpr = projSql;
                break;
            }
            case nameof(Enumerable.SelectMany):
            {
                string selSql = VisitLambdaAliased(call.Arguments[1], elementType, "e");
                crossJoin = $", json_each({selSql}) n";
                selectExpr = "n.value";
                break;
            }
            case nameof(Enumerable.Skip):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                offset = arg.SQLExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                break;
            }
            case nameof(Enumerable.Take):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                limit = arg.SQLExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                break;
            }
            case nameof(Enumerable.First) or nameof(Enumerable.FirstOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Last) or nameof(Enumerable.LastOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                if (orderBys.Count == 0)
                {
                    orderBys.Add("key DESC");
                }
                else
                {
                    for (int i = 0; i < orderBys.Count; i++)
                    {
                        orderBys[i] = orderBys[i].EndsWith(" ASC")
                            ? orderBys[i][..^4] + " DESC"
                            : orderBys[i][..^5] + " ASC";
                    }
                }

                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Count):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                selectExpr = "COUNT(*)";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Any):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                existsWrapper = "EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.All):
            {
                string predSql = VisitLambda(call.Arguments[1], elementType);
                wheres.Add($"NOT ({predSql})");
                existsWrapper = "NOT EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Min):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"MIN({selSql})";
                }
                else
                {
                    selectExpr = "MIN(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Max):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"MAX({selSql})";
                }
                else
                {
                    selectExpr = "MAX(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Sum):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"SUM({selSql})";
                }
                else
                {
                    selectExpr = "SUM(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Average):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"AVG({selSql})";
                }
                else
                {
                    selectExpr = "AVG(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Distinct):
            {
                distinct = true;
                break;
            }
            case nameof(Enumerable.Reverse):
            {
                if (orderBys.Count == 0)
                {
                    orderBys.Add("key DESC");
                }
                else
                {
                    for (int i = 0; i < orderBys.Count; i++)
                    {
                        orderBys[i] = orderBys[i].EndsWith(" ASC")
                            ? orderBys[i][..^4] + " DESC"
                            : orderBys[i][..^5] + " ASC";
                    }
                }

                break;
            }
            case nameof(Enumerable.ElementAt):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                string idxSql = arg.SQLExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                limit = "1";
                offset = idxSql;
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Contains):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                string valSql = arg.SQLExpression?.Sql ?? arg.Constant?.ToString() ?? "NULL";
                AddParameters(arg);
                wheres.Add($"value = {valSql}");
                existsWrapper = "EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Concat):
            case nameof(Enumerable.Union):
            case nameof(Enumerable.Intersect):
            case nameof(Enumerable.Except):
                break;
        }
    }

    private string BuildSql(string sourceSql)
    {
        string sp = new(' ', (visitor.Level + 1) * 4);
        string sp2 = new(' ', (visitor.Level + 2) * 4);
        string nl = Environment.NewLine;

        string distinctKeyword = distinct ? "DISTINCT " : "";
        string tableAlias = crossJoin != null ? " e" : "";
        string joinClause = crossJoin ?? "";

        List<string> clauses = [$"SELECT {distinctKeyword}{selectExpr}", $"FROM json_each({sourceSql}){tableAlias}{joinClause}"];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        if (groupBys.Count > 0)
        {
            clauses.Add("GROUP BY " + string.Join(", ", groupBys));
        }

        if (orderBys.Count > 0)
        {
            clauses.Add("ORDER BY " + string.Join(", ", orderBys));
        }

        if (limit != null && offset != null)
        {
            clauses.Add($"LIMIT {limit} OFFSET {offset}");
        }
        else if (limit != null)
        {
            clauses.Add($"LIMIT {limit}");
        }
        else if (offset != null)
        {
            clauses.Add($"LIMIT -1 OFFSET {offset}");
        }

        string innerSelect = string.Join(nl + sp, clauses);

        if (existsWrapper != null)
        {
            return $"{existsWrapper} ({nl}{sp}{innerSelect}{nl}{sp})";
        }

        if (wrapInArray)
        {
            bool needsSubquery = orderBys.Count > 0 || limit != null || offset != null;
            if (needsSubquery)
            {
                string innerSelect2 = string.Join(nl + sp2, clauses);
                return $"({nl}{sp}SELECT json_group_array({(distinct ? "DISTINCT " : "")}value){nl}{sp}FROM ({nl}{sp2}{innerSelect2}{nl}{sp}){nl})";
            }

            clauses[0] = $"SELECT json_group_array({distinctKeyword}{selectExpr})";
            string simpleSelect = string.Join(nl + sp, clauses);
            return $"({nl}{sp}{simpleSelect}{nl})";
        }

        return $"({nl}{sp}{innerSelect}{nl})";
    }

    private string VisitLambda(Expression arg, Type elementType)
    {
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, "value");

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLExpression sqlExpr)
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
        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(arg);
        ParameterExpression param = lambda.Parameters[0];

        BindParameter(param, elementType, $"{alias}.value");

        Expression result = visitor.Visit(lambda.Body);
        visitor.MethodArguments.Remove(param);

        if (result is SQLExpression sqlExpr)
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
        if (CommonHelpers.IsSimple(elementType, options))
        {
            SQLExpression valueExpr = new(elementType, -1, valueSql, (SQLiteParameter[]?)null);
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

            if (CommonHelpers.IsSimple(prop.PropertyType, options))
            {
                string? sql = null;
                foreach (SQLitePropertyTranslator translator in options.PropertyTranslators)
                {
                    sql = translator(dictKey, valueSql);
                    if (sql != null)
                    {
                        break;
                    }
                }

                if (sql != null)
                {
                    dict[dictKey] = new SQLExpression(prop.PropertyType, -1, sql, (SQLiteParameter[]?)null);
                }
            }
            else
            {
                RegisterProperties(prop.PropertyType, dictKey, valueSql, dict);
            }
        }
    }

    private void AddParameters(ResolvedModel model)
    {
        if (model.SQLExpression?.Parameters != null)
        {
            parameters.AddRange(model.SQLExpression.Parameters);
        }
    }

    private static Type CoerceType(Type declaredType, Type sourceType, SQLiteStorageOptions options)
    {
        if (!options.TypeConverters.ContainsKey(sourceType))
        {
            return declaredType;
        }

        if (declaredType.IsAssignableFrom(sourceType))
        {
            return sourceType;
        }

        if (CommonHelpers.GetEnumerableElementType(declaredType) is Type declaredElem
            && CommonHelpers.GetEnumerableElementType(sourceType) is Type sourceElem
            && declaredElem == sourceElem)
        {
            return sourceType;
        }

        return declaredType;
    }
}
