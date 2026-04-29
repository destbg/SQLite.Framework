using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests.Helpers;

/// <summary>
/// Wraps a string-shaped translator (instance, args) =&gt; sql into the new
/// SQLiteMemberTranslator (ctx) =&gt; Expression API. Convenience for tests written against
/// the legacy API that just want to format some SQL around the resolved sub-expressions.
/// </summary>
internal static class SimpleTranslator
{
    public static SQLiteMemberTranslator AsPredicate(Func<string?, string, string> build)
    {
        return ctx =>
        {
            MethodCallExpression node = (MethodCallExpression)ctx.Node;

            bool isStatic = node.Object == null;
            Expression instanceExpr = isStatic ? node.Arguments[0] : node.Object!;
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(
                isStatic ? node.Arguments[1] : node.Arguments[0]);

            if (ctx.Visit(instanceExpr) is not SQLiteExpression instanceSql)
            {
                return node;
            }

            ParameterExpression parameter = lambda.Parameters[0];
            SQLiteExpression valueExpr = new(parameter.Type, -1, "value", (SQLiteParameter[]?)null);
            ctx.MethodArguments[parameter] = new Dictionary<string, Expression> { [""] = valueExpr };

            Expression predicateResult;
            try
            {
                predicateResult = ctx.Visit(lambda.Body);
            }
            finally
            {
                ctx.MethodArguments.Remove(parameter);
            }

            if (predicateResult is not SQLiteExpression predicateSql)
            {
                return node;
            }

            string sql = build(instanceSql.Sql, predicateSql.Sql);
            SQLiteParameter[] parameters =
                [.. instanceSql.Parameters ?? [], .. predicateSql.Parameters ?? []];
            return new SQLiteExpression(node.Method.ReturnType, ctx.Counters.IdentifierIndex++, sql, parameters);
        };
    }

    public static SQLiteMemberTranslator AsSimple(Func<string?, string[], string> build)
    {
        return ctx =>
        {
            MethodCallExpression node = (MethodCallExpression)ctx.Node;
            SQLVisitor visitor = ctx.Visitor;

            ResolvedModel? objModel = node.Object != null
                ? visitor.ResolveExpression(node.Object)
                : null;

            List<ResolvedModel> argModels = node.Arguments
                .Select(visitor.ResolveExpression)
                .ToList();

            if (argModels.Any(a => a.SQLiteExpression == null)
                || (objModel is { SQLiteExpression: null }))
            {
                return objModel is { } o
                    ? Expression.Call(o.Expression, node.Method, argModels.Select(a => a.Expression))
                    : Expression.Call(node.Method, argModels.Select(a => a.Expression));
            }

            string sql = build(objModel?.Sql, argModels.Select(a => a.Sql!).ToArray());

            List<SQLiteExpression> all = argModels.Select(a => a.SQLiteExpression!).ToList();
            if (objModel is { SQLiteExpression: { } objSql })
            {
                all.Insert(0, objSql);
            }

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(all);

            return new SQLiteExpression(
                node.Method.ReturnType,
                ctx.Counters.IdentifierIndex++,
                sql,
                parameters);
        };
    }
}
