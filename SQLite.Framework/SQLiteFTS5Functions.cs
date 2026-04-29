namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite FTS5 full-text search. These methods throw at runtime and are only
/// valid inside a LINQ query where they are translated to FTS5 SQL. Use them on tables mapped
/// to FTS5 via <see cref="Attributes.FullTextSearchAttribute" />.
/// </summary>
[ExcludeFromCodeCoverage]
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android24.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios10.0")]
#endif
public static class SQLiteFTS5Functions
{
    private const string OutsideQuery = "SQLiteFTS5Functions methods can only be used inside a LINQ query.";

    /// <summary>
    /// Matches the entity's whole row against an FTS5 query string. Translates to
    /// <c>&lt;table&gt; MATCH '&lt;query&gt;'</c>. Use this on tables that are mapped to FTS5.
    /// </summary>
    public static bool Match<T>(T entity, string query)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Matches a single column against an FTS5 query string. Translates to
    /// <c>&lt;table&gt; MATCH '{Column} : &lt;query&gt;'</c>.
    /// </summary>
    public static bool Match(string column, string query)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Matches the entity's whole row against a builder-style FTS5 query. The lambda gets a
    /// <see cref="SQLiteFTS5Builder" /> on which you call <c>Term</c>, <c>Phrase</c>, <c>Prefix</c>,
    /// <c>Near</c>, and <c>Column</c>, joined with C# <c>&amp;&amp;</c>, <c>||</c>, and <c>!</c>.
    /// </summary>
    public static bool Match<T>(T entity, Func<SQLiteFTS5Builder, bool> predicate)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Matches a single column against a builder-style FTS5 query.
    /// </summary>
    public static bool Match(string column, Func<SQLiteFTS5Builder, bool> predicate)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the BM25 score of the current row for the active FTS5 match. Use it inside
    /// <c>OrderBy</c> to rank results by how well they match.
    /// </summary>
    public static double Rank<T>(T entity)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a snippet of <paramref name="column" /> with matching tokens wrapped in
    /// <paramref name="before" /> and <paramref name="after" /> markers. Translates to FTS5's
    /// <c>snippet()</c> auxiliary function.
    /// </summary>
    public static string Snippet<T>(T entity, string column, string before, string after, string ellipsis, int tokens)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the value of <paramref name="column" /> with each matching token wrapped in
    /// <paramref name="before" /> and <paramref name="after" /> markers. Translates to FTS5's
    /// <c>highlight()</c> auxiliary function.
    /// </summary>
    public static string Highlight<T>(T entity, string column, string before, string after)
    {
        throw new InvalidOperationException(OutsideQuery);
    }
}
