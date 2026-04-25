using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite functions that have no plain C# equivalent. These methods throw at
/// runtime. They are only valid inside a LINQ query, where the framework swaps them for the right
/// SQL. Examples are <c>RANDOM()</c>, <c>GLOB</c>, <c>REGEXP</c>, and the FTS5 helpers
/// (<c>MATCH</c>, <c>RANK</c>, <c>SNIPPET</c>, <c>HIGHLIGHT</c>).
/// </summary>
[ExcludeFromCodeCoverage]
public static class SQLiteFunctions
{
    private const string OutsideQuery = "SQLiteFunctions methods can only be used inside a LINQ query.";

    /// <summary>
    /// Returns a pseudo-random number between 0 and 1, exclusive of both ends. Translates to
    /// SQLite's <c>RANDOM()</c>.
    /// </summary>
    public static double Random()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a blob of <paramref name="n" /> random bytes. Translates to SQLite's
    /// <c>RANDOMBLOB(n)</c>.
    /// </summary>
    public static byte[] RandomBlob(int n)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns true when <paramref name="value" /> matches the given GLOB <paramref name="pattern" />.
    /// Translates to SQLite's <c>value GLOB pattern</c>. GLOB uses the same wildcards as Unix shell:
    /// <c>*</c> matches any string, <c>?</c> matches one character.
    /// </summary>
    public static bool Glob(string pattern, string value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the current Unix timestamp in seconds. Translates to SQLite's <c>unixepoch()</c>.
    /// </summary>
    public static long UnixEpoch()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Parses <paramref name="when" /> as a date or time string and returns its Unix timestamp in
    /// seconds. Translates to SQLite's <c>unixepoch(when)</c>.
    /// </summary>
    public static long UnixEpoch(string when)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Formats <paramref name="args" /> using the C-style <paramref name="format" /> string.
    /// Translates to SQLite's <c>printf(format, ...)</c>.
    /// </summary>
    public static string Printf(string format, params object[] args)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns true when <paramref name="value" /> matches the given regular expression
    /// <paramref name="pattern" />. Translates to SQLite's <c>value REGEXP pattern</c>. SQLite
    /// only supports this when a regex extension is loaded.
    /// </summary>
    public static bool Regexp(string value, string pattern)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the number of database rows that the most recent SQL statement changed. Translates
    /// to SQLite's <c>changes()</c>.
    /// </summary>
    public static long Changes()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the total number of rows changed since the connection was opened. Translates to
    /// SQLite's <c>total_changes()</c>.
    /// </summary>
    public static long TotalChanges()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

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
