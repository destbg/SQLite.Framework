using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a LINQ expression tree before translation and rewrites every <c>Constant(SQLiteTable&lt;E&gt;)</c>
/// reference to <c>Table&lt;E&gt;().Where(filter1).Where(filter2)...</c> when the user has registered
/// <see cref="SQLiteOptions.QueryFilters" /> that apply to <c>E</c>. A subtree under
/// <see cref="QueryableExtensions.IgnoreQueryFilters{T}" /> is processed with injection disabled so
/// the user can opt out per query, including inside subqueries (such as <c>Join</c>).
/// </summary>
internal sealed class QueryFilterInjector : ExpressionVisitor
{
    private readonly SQLiteOptions options;
    private bool ignoreFilters;

    private QueryFilterInjector(SQLiteOptions options)
    {
        this.options = options;
    }

    public static Expression Inject(Expression source, SQLiteOptions options)
    {
        if (options.QueryFilters.Count == 0)
        {
            return source;
        }

        QueryFilterInjector injector = new(options);
        return injector.Visit(source) ?? source;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The element type comes from a SQLiteTable<T> instance whose type was preserved via DynamicallyAccessedMembers.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Queryable.Where is rooted by user code that already calls Where.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (ignoreFilters || node.Value is not BaseSQLiteTable table)
        {
            return node;
        }

        Type entityType = table.ElementType;
        Expression result = node;

        foreach (KeyValuePair<Type, IReadOnlyList<LambdaExpression>> kvp in options.QueryFilters)
        {
            if (!kvp.Key.IsAssignableFrom(entityType))
            {
                continue;
            }

            foreach (LambdaExpression filter in kvp.Value)
            {
                LambdaExpression rebound = QueryFilterRebinder.Rebind(filter, entityType);
                result = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Where),
                    [entityType],
                    result,
                    Expression.Quote(rebound));
            }
        }

        return result;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsIgnoreQueryFiltersCall(node))
        {
            bool previous = ignoreFilters;
            ignoreFilters = true;
            try
            {
                return Visit(node.Arguments[0]) ?? node.Arguments[0];
            }
            finally
            {
                ignoreFilters = previous;
            }
        }

        return base.VisitMethodCall(node);
    }

    private static bool IsIgnoreQueryFiltersCall(MethodCallExpression node)
    {
        return node.Method.IsStatic
            && node.Method.DeclaringType == typeof(QueryableExtensions)
            && node.Method.Name == nameof(QueryableExtensions.IgnoreQueryFilters);
    }
}
