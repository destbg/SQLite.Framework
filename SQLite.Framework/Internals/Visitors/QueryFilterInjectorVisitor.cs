namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a LINQ expression tree before translation and rewrites every <c>Constant(SQLiteTable&lt;E&gt;)</c>
/// reference to <c>Table&lt;E&gt;().Where(filter1).Where(filter2)...</c> when the user has registered
/// <see cref="SQLiteOptions.QueryFilters" /> that apply to <c>E</c>. The filters come from the
/// database that owns the table, so a table read from an attached database uses the filters
/// registered on that database. A subtree under
/// <see cref="QueryableExtensions.IgnoreQueryFilters{T}" /> is processed with injection disabled so
/// the user can opt out per query, including inside subqueries (such as <c>Join</c>).
/// </summary>
internal sealed class QueryFilterInjectorVisitor : ExpressionVisitor
{
    private readonly SQLiteOptions options;
    private readonly HashSet<(Type EntityType, SQLiteOptions Options)> injecting = [];
    private bool ignoreFilters;

    public QueryFilterInjectorVisitor(SQLiteOptions options, bool ignoreAll)
    {
        this.options = options;
        ignoreFilters = ignoreAll;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Element type is preserved by SQLiteTable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Queryable.Where is rooted by user code that already calls Where.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (ignoreFilters || node.Value is not BaseSQLiteTable table)
        {
            return node;
        }

        return InjectFilters(node, table.ElementType, table.Database.Options);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Element type is preserved by SQLiteTable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Queryable.Where is rooted by user code that already calls Where.")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (!ignoreFilters
            && node.Type.IsAssignableTo(typeof(IQueryable))
            && ExpressionHelpers.IsConstant(node)
            && ExpressionHelpers.GetConstantValue(node) is BaseSQLiteTable table)
        {
            Expression source = typeof(IQueryable<>).MakeGenericType(table.ElementType).IsAssignableFrom(node.Type)
                ? node
                : Expression.Constant(table);
            return InjectFilters(source, table.ElementType, table.Database.Options);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (QueryFilterInjector.IsIgnoreQueryFiltersCall(node))
        {
            bool previous = ignoreFilters;
            ignoreFilters = true;
            try
            {
                return Visit(node.Arguments[0]);
            }
            finally
            {
                ignoreFilters = previous;
            }
        }

        if (!ignoreFilters
            && typeof(BaseSQLiteTable).IsAssignableFrom(node.Type)
            && node.Type.IsGenericType)
        {
            Type entityType = node.Type.GetGenericArguments()[0];
            return InjectFilters(node, entityType, ResolveOwnerOptions(node));
        }

        return base.VisitMethodCall(node);
    }

    private SQLiteOptions ResolveOwnerOptions(MethodCallExpression node)
    {
        if (node.Object != null
            && ExpressionHelpers.IsConstant(node.Object)
            && ExpressionHelpers.GetConstantValue(node.Object) is SQLiteDatabase owner)
        {
            return owner.Options;
        }

        return options;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Element type is preserved by SQLiteTable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Queryable.Where is rooted by user code that already calls Where.")]
    private Expression InjectFilters(Expression source, Type entityType, SQLiteOptions filterOptions)
    {
        if (!injecting.Add((entityType, filterOptions)))
        {
            return source;
        }

        try
        {
            Expression result = source;

            foreach (KeyValuePair<Type, IReadOnlyList<LambdaExpression>> kvp in filterOptions.QueryFilters)
            {
                if (!kvp.Key.IsAssignableFrom(entityType))
                {
                    continue;
                }

                foreach (LambdaExpression filter in kvp.Value)
                {
                    LambdaExpression rebound = CommonHelpers.Rebind(filter, entityType);
                    Expression injectedBody = Visit(rebound.Body);
                    LambdaExpression injected = Expression.Lambda(injectedBody, rebound.Parameters);
                    result = Expression.Call(
                        typeof(System.Linq.Queryable),
                        nameof(System.Linq.Queryable.Where),
                        [entityType],
                        result,
                        Expression.Quote(injected));
                }
            }

            return result;
        }
        finally
        {
            injecting.Remove((entityType, filterOptions));
        }
    }
}
