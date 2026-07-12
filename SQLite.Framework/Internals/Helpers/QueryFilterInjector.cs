namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Injects registered <see cref="SQLiteOptions.QueryFilters" /> into a LINQ expression tree before translation.
/// </summary>
internal static class QueryFilterInjector
{
    public static Expression Inject(Expression source, SQLiteOptions options, bool ignoreAll)
    {
        QueryFilterInjectorVisitor injector = new(options, ignoreAll);
        return injector.Visit(source);
    }

    public static bool ShouldIgnoreAll(Expression source, SQLiteDatabase database)
    {
        return HasReachableQueryFilters(database) && ContainsIgnoreFilters(source);
    }

    public static Expression InjectCteBody(Expression body, SQLiteDatabase database, SQLiteCounters counters)
    {
        return Inject(body, database.Options, counters.IgnoreQueryFilters || ShouldIgnoreAll(body, database));
    }

    public static bool IsIgnoreQueryFiltersCall(MethodCallExpression node)
    {
        return node.Method.IsStatic
            && node.Method.DeclaringType == typeof(QueryableExtensions)
            && node.Method.Name == nameof(QueryableExtensions.IgnoreQueryFilters);
    }

    private static bool HasReachableQueryFilters(SQLiteDatabase database)
    {
        return database.Options.QueryFilters.Count > 0 || database.AnyAttachedDatabaseHasQueryFilters();
    }

    private static bool ContainsIgnoreFilters(Expression source)
    {
        IgnoreQueryFiltersDetectorVisitor detector = new();
        detector.Visit(source);
        return detector.Found;
    }
}
