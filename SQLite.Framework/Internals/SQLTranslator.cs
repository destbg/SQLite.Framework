using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Extensions;
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
    private readonly List<SQLiteParameter> parameters = [];
    private readonly QueryableMethodVisitor queryableMethodVisitor;
    private readonly SQLVisitor visitor;
    private readonly int level;

    public SQLTranslator(SQLiteDatabase database)
    {
        ParameterIndexWrapper paramIndex = new();
        TableIndexWrapper tableIndex = new();

        visitor = new SQLVisitor(database, paramIndex, tableIndex, level);
        queryableMethodVisitor = new QueryableMethodVisitor(database, visitor);
    }

    public SQLTranslator(SQLiteDatabase database, ParameterIndexWrapper paramIndex, TableIndexWrapper tableIndex, int level)
    {
        this.level = level;
        visitor = new SQLVisitor(database, paramIndex, tableIndex, level);
        queryableMethodVisitor = new QueryableMethodVisitor(database, visitor);
    }

    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments
    {
        get => visitor.MethodArguments;
        init => visitor.MethodArguments = value;
    }

    public bool IsInnerQuery
    {
        get => queryableMethodVisitor.IsInnerQuery;
        set => queryableMethodVisitor.IsInnerQuery = value;
    }

    public SQLQuery Translate(Expression node)
    {
        Expression? selectMethod;
        if (node is MethodCallExpression mce)
        {
            selectMethod = TranslateMethodExpression(mce);
        }
        else
        {
            selectMethod = TranslateOtherExpression(node);
        }

        if (visitor.From == null)
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

        // FROM
        string from = $"FROM {visitor.From}";

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
        string joinSql = string.Join(Environment.NewLine + spacing, queryableMethodVisitor.Joins.Select(j =>
            $"{j.JoinType} {j.Sql} ON {j.OnClause}"));

        foreach (JoinInfo join in queryableMethodVisitor.Joins)
        {
            VisitSQLExpression(join.Sql);
            VisitSQLExpression(join.OnClause);
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
            ? string.Join(" AND ", queryableMethodVisitor.Havings)
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
            $"SELECT{distinct} {(useExists ? "1" : selectSql)}",
            from,
            joinSql,
            whereSql,
            groupBySql,
            havingSql,
            orderBy,
            limit,
            offset,
        }.Where(f => !string.IsNullOrEmpty(f)));

        if (queryableMethodVisitor.Unions.Count > 0)
        {
            foreach ((SQLExpression sqlExpression, bool _) in queryableMethodVisitor.Unions)
            {
                VisitSQLExpression(sqlExpression);
            }

            string unions = string.Join(Environment.NewLine + spacing, queryableMethodVisitor.Unions.Select(f =>
                $"{spacing}UNION{(f.All ? " ALL" : string.Empty)}{Environment.NewLine}{spacing}{f.Sql}"));

            sql = $"{sql}{Environment.NewLine}{unions}";
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
        if (selectMethod is null or ParameterExpression or MemberExpression or MethodCallExpression)
        {
            createObject = null;
        }
        else
        {
            QueryCompilerVisitor compilerVisitor = new();
            CompiledExpression compiledExpression = (CompiledExpression)compilerVisitor.Visit(selectMethod);
            createObject = compiledExpression.Call;
        }

        return new SQLQuery
        {
            Sql = sql,
            Parameters = parameters,
            CreateObject = createObject,
            ThrowOnEmpty = queryableMethodVisitor.ThrowOnEmpty,
            ThrowOnMoreThanOne = queryableMethodVisitor.ThrowOnMoreThanOne,
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
        if (mce.Method.DeclaringType != typeof(Queryable) && mce.Method.DeclaringType != typeof(QueryableExtensions))
        {
            throw new NotSupportedException($"Unsupported method: {mce.Method}");
        }

        List<MethodCallExpression> methodCalls = [];
        MethodCallExpression callExpression = mce;

        while (true)
        {
            methodCalls.Add(callExpression);
            if (callExpression.Arguments.Count == 0
                || callExpression.Method.Name is nameof(Queryable.Concat) or nameof(Queryable.Union))
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
                visitor.AssignTable(callExpression.Method.GetGenericArguments()[0]);
                methodCalls.RemoveAt(methodCalls.Count - 1);
            }
            else
            {
                throw new NotSupportedException($"Unsupported method: {callExpression.Method}");
            }
        }
        else if (callExpression.Method.Name is nameof(Queryable.Concat) or nameof(Queryable.Union))
        {
            visitor.Visit(callExpression.Arguments[1]);
        }
        else
        {
            visitor.Visit(callExpression.Arguments[0]);
        }

        if (methodCalls.All(f => !IsSelectMethod(f.Method)))
        {
            MethodCallExpression selectMethod = CreateIdentitySelectExpression(methodCalls[0].Type);
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
                visitor.MethodArguments[lambdaExpression.Parameters[0]] = visitor.TableColumns;
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
        Type genericType = expression.Type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>))
            ?.GenericTypeArguments[0] ?? throw new InvalidOperationException("Expression is not an IQueryable.");

        MethodCallExpression selectMethod = CreateIdentitySelectExpression(genericType);

        UnaryExpression unaryExpression = selectMethod.Arguments
            .Skip(1)
            .OfType<UnaryExpression>()
            .First();
        LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;

        visitor.Visit(expression);

        visitor.MethodArguments[lambdaExpression.Parameters[0]] = visitor.TableColumns;

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
            new[] { genericType, genericType },
            sourceParameter,
            selector
        );
    }

    private static bool IsSelectMethod(MethodInfo method)
    {
        return method.GetParameters().Length > 0 && (
            method.Name is nameof(Queryable.Select)
                or nameof(Queryable.Min)
                or nameof(Queryable.Max)
                or nameof(Queryable.Sum)
                or nameof(Queryable.Count)
                or nameof(Queryable.Average)
                or nameof(Queryable.Contains)
        );
    }
}