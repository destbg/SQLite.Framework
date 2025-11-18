using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals;

/// <summary>
/// Translates LINQ expressions into SQL queries.
/// </summary>
/// <remarks>
/// This class gets the different parts of the LINQ expression tree and translates them into SQL.
/// </remarks>
internal class SQLTranslator
{
    private readonly int level;
    private readonly List<SQLiteParameter> parameters = [];
    private readonly QueryableMethodVisitor queryableMethodVisitor;
    private Expression? selectMethodExpression;

    public SQLTranslator(SQLiteDatabase database)
    {
        IndexWrapper paramIndex = new();
        IndexWrapper identifierIndex = new();
        TableIndexWrapper tableIndex = new();

        Visitor = new SQLVisitor(database, paramIndex, identifierIndex, tableIndex, level);
        queryableMethodVisitor = new QueryableMethodVisitor(database, Visitor);
    }

    public SQLTranslator(SQLiteDatabase database, IndexWrapper paramIndex, IndexWrapper identifierIndex, TableIndexWrapper tableIndex, int level)
    {
        this.level = level;
        Visitor = new SQLVisitor(database, paramIndex, identifierIndex, tableIndex, level);
        queryableMethodVisitor = new QueryableMethodVisitor(database, Visitor);
    }

    public SQLVisitor Visitor { get; }

    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments
    {
        init => Visitor.MethodArguments = value;
    }

    public bool IsInnerQuery
    {
        set => queryableMethodVisitor.IsInnerQuery = value;
    }

    public QueryType QueryType { get; init; }

    public List<(string Name, SQLExpression Expression)>? SetProperties { get; set; }

    public void Visit(Expression node)
    {
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

        string spacing = new(' ', level * 4);

        if (queryableMethodVisitor.Joins.Any(f => f.IsGroupJoin))
        {
            throw new NotSupportedException("Group joins that are not turned into LEFT JOIN are not supported.");
        }

        bool useExists = queryableMethodVisitor.IsAny || queryableMethodVisitor.IsAll;

        // SELECT
        string select;

        if (QueryType == QueryType.Select)
        {
            string distinct = queryableMethodVisitor.IsDistinct ? " DISTINCT" : string.Empty;

            string selectSql = queryableMethodVisitor.Selects.Count > 0 && !useExists
                ? string.Join($",{Environment.NewLine}       ", queryableMethodVisitor.Selects.Select(f => $"{f.Sql} AS \"{f.IdentifierText}\""))
                : "*";

            if (!useExists)
            {
                foreach (SQLExpression expression in queryableMethodVisitor.Selects)
                {
                    VisitSQLExpression(expression);
                }
            }

            select = $"SELECT{distinct} {(useExists ? "1" : selectSql)}";
        }
        else
        {
            select = string.Empty;
        }

        // FROM
        string from = QueryType switch
        {
            QueryType.Delete => $"DELETE FROM {Visitor.From.Sql}",
            QueryType.Update => $"UPDATE {Visitor.From.Sql}",
            _ => $"FROM {Visitor.From.Sql}"
        };

        VisitSQLExpression(Visitor.From);

        // SET
        string set = QueryType == QueryType.Update && SetProperties != null
            ? $"SET {string.Join(", ", SetProperties.Select(f => $"{f.Name} = {f.Expression.Sql}"))}"
            : string.Empty;

        if (QueryType == QueryType.Update && SetProperties != null)
        {
            foreach ((string _, SQLExpression sqlExpression) in SetProperties)
            {
                VisitSQLExpression(sqlExpression);
            }
        }

        // WHERE
        string whereSql = queryableMethodVisitor.Wheres.Count > 0
            ? "WHERE " + (queryableMethodVisitor.IsAll
                ? $"NOT ({string.Join(" AND ", queryableMethodVisitor.Wheres)})"
                : string.Join(" AND ", queryableMethodVisitor.Wheres))
            : string.Empty;

        foreach (SQLExpression sqlExpression in queryableMethodVisitor.Wheres)
        {
            VisitSQLExpression(sqlExpression);
        }

        // JOINs
        string joinSql = string.Join(Environment.NewLine + spacing,
            queryableMethodVisitor.Joins.Select(j =>
                j.OnClause != null
                    ? $"{j.JoinType} {j.Sql} ON {j.OnClause}"
                    : $"{j.JoinType} {j.Sql}"
            )
        );

        foreach (JoinInfo join in queryableMethodVisitor.Joins)
        {
            VisitSQLExpression(join.Sql);

            if (join.OnClause != null)
            {
                VisitSQLExpression(join.OnClause);
            }
        }

        // GROUP BY
        string groupBySql = queryableMethodVisitor.GroupBys.Count > 0
            ? "GROUP BY " + string.Join(", ", queryableMethodVisitor.GroupBys)
            : string.Empty;

        foreach (SQLExpression sqlExpression in queryableMethodVisitor.GroupBys)
        {
            VisitSQLExpression(sqlExpression);
        }

        // HAVING
        string havingSql = queryableMethodVisitor.Havings.Count > 0
            ? "HAVING " + string.Join(" AND ", queryableMethodVisitor.Havings)
            : string.Empty;

        foreach (SQLExpression sqlExpression in queryableMethodVisitor.Havings)
        {
            VisitSQLExpression(sqlExpression);
        }

        // ORDER BY
        string orderBy = queryableMethodVisitor.OrderBys.Count > 0 && !useExists
            ? "ORDER BY " + string.Join(", ", queryableMethodVisitor.OrderBys)
            : string.Empty;

        foreach (SQLExpression sqlExpression in queryableMethodVisitor.OrderBys)
        {
            VisitSQLExpression(sqlExpression);
        }

        // LIMIT
        string limit = queryableMethodVisitor.Take != null
            ? $"LIMIT {queryableMethodVisitor.Take}"
            : queryableMethodVisitor.Skip != null
                ? "LIMIT -1"
                : string.Empty;

        // OFFSET
        string offset = queryableMethodVisitor.Skip != null
            ? $"OFFSET {queryableMethodVisitor.Skip}"
            : string.Empty;

        string sql = spacing + string.Join(Environment.NewLine + spacing, new[]
        {
            select,
            from,
            joinSql,
            set,
            whereSql,
            groupBySql,
            havingSql,
            orderBy,
            limit,
            offset
        }.Where(f => !string.IsNullOrEmpty(f)));

        // UNION, UNION ALL, INTERSECT, EXCEPT
        if (queryableMethodVisitor.SetOperations.Count > 0)
        {
            foreach ((SQLExpression sqlExpression, string _) in queryableMethodVisitor.SetOperations)
            {
                VisitSQLExpression(sqlExpression);
            }

            IEnumerable<string> list = queryableMethodVisitor.SetOperations.Select(f =>
                $"{spacing}{f.Type}{Environment.NewLine}{spacing}{f.Sql}");

            string setOperations = string.Join(Environment.NewLine + spacing, list);

            sql = $"{sql}{Environment.NewLine}{setOperations}";
        }

        if (queryableMethodVisitor.IsAny)
        {
            sql = $"{spacing}SELECT EXISTS({sql.Trim()}) as result";
        }
        else if (queryableMethodVisitor.IsAll)
        {
            sql = $"{spacing}SELECT NOT EXISTS({sql.Trim()}) as result";
        }

        Func<QueryContext, dynamic?>? createObject;
        if (selectMethodExpression is null or ParameterExpression or MemberExpression or MethodCallExpression)
        {
            createObject = null;
        }
        else
        {
            QueryCompilerVisitor compilerVisitor = new();
            CompiledExpression compiledExpression = (CompiledExpression)compilerVisitor.Visit(selectMethodExpression);
            createObject = compiledExpression.Call;
        }

        return new SQLQuery
        {
            Sql = sql,
            Parameters = parameters,
            CreateObject = createObject,
            Reverse = queryableMethodVisitor.Reverse,
            ThrowOnEmpty = queryableMethodVisitor.ThrowOnEmpty,
            ThrowOnMoreThanOne = queryableMethodVisitor.ThrowOnMoreThanOne
        };
    }

    private void VisitSQLExpression(SQLExpression node)
    {
        if (node.Parameters != null)
        {
            parameters.AddRange(node.Parameters);
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
                    ? CommonHelpers.GetConstantValue(callExpression.Object!)
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

        if (methodCalls.All(f => !IsSelectMethod(f.Method)))
        {
            if (methodCalls[0].Type.IsAssignableTo(typeof(IQueryable)))
            {
                MethodCallExpression selectMethod = CreateIdentitySelectExpression(methodCalls[0].Type.GetGenericArguments()[0]);
                methodCalls.Insert(0, selectMethod);
            }
            else
            {
                MethodCallExpression selectMethod = CreateIdentitySelectExpression(methodCalls[0].Type);
                methodCalls.Insert(0, selectMethod);
            }
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

        if (expression.Type.IsAssignableTo(typeof(BaseSQLiteTable)) && CommonHelpers.IsConstant(expression))
        {
            BaseSQLiteTable table = (BaseSQLiteTable)CommonHelpers.GetConstantValue(expression)!;
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

        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(selectMethod.Arguments[1]);
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