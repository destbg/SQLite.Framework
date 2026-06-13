namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Injects registered <see cref="SQLiteOptions.QueryFilters" /> into a LINQ expression tree before translation.
/// </summary>
internal static class QueryFilterInjector
{
    public static Expression Inject(Expression source, SQLiteOptions options)
    {
        QueryFilterInjectorVisitor injector = new(options);
        return injector.Visit(source);
    }
}
