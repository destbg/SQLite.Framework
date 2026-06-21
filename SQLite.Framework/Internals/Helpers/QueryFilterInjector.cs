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

    public static bool ShouldIgnoreAll(Expression source, SQLiteOptions options)
    {
        return options.QueryFilters.Count > 0 && ContainsIgnoreFilters(source);
    }

    public static Expression InjectCteBody(Expression body, SQLiteOptions options, SQLiteCounters counters)
    {
        return Inject(body, options, counters.IgnoreQueryFilters || ShouldIgnoreAll(body, options));
    }

    public static bool IsIgnoreQueryFiltersCall(MethodCallExpression node)
    {
        return node.Method.IsStatic
            && node.Method.DeclaringType == typeof(QueryableExtensions)
            && node.Method.Name == nameof(QueryableExtensions.IgnoreQueryFilters);
    }

    private static bool ContainsIgnoreFilters(Expression source)
    {
        IgnoreQueryFiltersDetectorVisitor detector = new();
        detector.Visit(source);
        return detector.Found;
    }
}
