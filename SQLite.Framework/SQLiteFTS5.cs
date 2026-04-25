using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite FTS5 full-text search. These methods throw at runtime and are
/// only valid inside a LINQ query, where they are translated to their SQL equivalents.
/// FTS5 is built into the SQLite versions that ship with this framework's NuGet packages, so
/// no separate registration call is needed.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SQLiteFTS5
{
    private const string OutsideQuery = "SQLiteFTS5 methods can only be used inside a LINQ query against an FTS5-mapped table.";

    /// <summary>
    /// Matches the entity's whole row against an FTS5 query string. Translates to
    /// <c>&lt;table&gt; MATCH '&lt;query&gt;'</c>.
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
    /// Matches the entity's whole row against a builder-style FTS5 query. The lambda receives an
    /// <see cref="SQLiteFTS5Builder" /> on which you call <c>Term</c>, <c>Phrase</c>, <c>Prefix</c>,
    /// <c>Near</c>, and <c>Column</c>, combined with C# <c>&amp;&amp;</c>, <c>||</c>, and <c>!</c>.
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
    /// Returns the BM25 score of the current row for the active FTS5 match. Use inside
    /// <c>OrderBy</c> to rank results by relevance.
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
