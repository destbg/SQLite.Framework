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
    /// Returns true when <paramref name="value" /> is between <paramref name="low" /> and
    /// <paramref name="high" /> inclusive. Translates to SQLite's
    /// <c>value BETWEEN low AND high</c>.
    /// To express the negated form, wrap the call with <c>!</c> (or compare to <c>false</c>);
    /// the framework emits <c>NOT (value BETWEEN low AND high)</c>, which SQLite treats
    /// identically to <c>NOT BETWEEN</c>.
    /// </summary>
    public static bool Between<T>(T value, T low, T high)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns true when <paramref name="value" /> equals any element of <paramref name="values" />.
    /// Translates to SQLite's <c>value IN (v0, v1, ...)</c>. Negate with <c>!</c> for
    /// <c>NOT IN</c>-equivalent semantics.
    /// </summary>
    public static bool In<T>(T value, params T[] values)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the first non-null value among <paramref name="values" />, or <see langword="null" />
    /// if all are null. Translates to SQLite's <c>coalesce(v0, v1, ...)</c>. Pass at least two
    /// arguments; SQLite requires it.
    /// </summary>
    public static T? Coalesce<T>(params T?[] values)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns <see langword="null" /> when <paramref name="a" /> equals <paramref name="b" />,
    /// otherwise returns <paramref name="a" />. Translates to SQLite's <c>nullif(a, b)</c>.
    /// </summary>
    public static T? Nullif<T>(T a, T b)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the SQLite storage class of <paramref name="value" /> as a lowercase string
    /// (<c>"null"</c>, <c>"integer"</c>, <c>"real"</c>, <c>"text"</c>, or <c>"blob"</c>).
    /// Translates to SQLite's <c>typeof(value)</c>.
    /// </summary>
    public static string Typeof<T>(T value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the upper-case hexadecimal encoding of the BLOB <paramref name="value" />.
    /// Translates to SQLite's <c>hex(value)</c>.
    /// </summary>
    public static string Hex(byte[] value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a SQL-quoted string literal representation of <paramref name="value" />, suitable
    /// for inclusion in a SQL statement. Translates to SQLite's <c>quote(value)</c>.
    /// </summary>
    public static string Quote<T>(T value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a BLOB of <paramref name="length" /> zero bytes. Translates to SQLite's
    /// <c>zeroblob(length)</c>.
    /// </summary>
    public static byte[] Zeroblob(long length)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the 1-based index of the first occurrence of <paramref name="needle" /> within
    /// <paramref name="haystack" />, or 0 when not found. Translates to SQLite's
    /// <c>instr(haystack, needle)</c>.
    /// </summary>
    public static int Instr(string haystack, string needle)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the rowid of the most recent successful INSERT on the connection. Translates to
    /// SQLite's <c>last_insert_rowid()</c>.
    /// </summary>
    public static long LastInsertRowId()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the SQLite library version string. Translates to SQLite's <c>sqlite_version()</c>.
    /// </summary>
    public static string SqliteVersion()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the per-row minimum of the supplied <paramref name="values" />. Translates to
    /// SQLite's scalar <c>min(v0, v1, ...)</c>. Always pass at least two values.
    ///
    /// Be careful: passing a single value will compile fine but produce wrong results. SQLite
    /// reads <c>min(x)</c> as the aggregate <c>MIN</c>, which silently turns the whole query
    /// into an aggregate query and returns one row instead of one per input row. Use LINQ's
    /// <c>Queryable.Min</c> for the aggregate form.
    /// </summary>
    public static T Min<T>(params T[] values)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the per-row maximum of the supplied <paramref name="values" />. Translates to
    /// SQLite's scalar <c>max(v0, v1, ...)</c>. Always pass at least two values.
    ///
    /// Be careful: passing a single value will compile fine but produce wrong results. SQLite
    /// reads <c>max(x)</c> as the aggregate <c>MAX</c>, which silently turns the whole query
    /// into an aggregate query and returns one row instead of one per input row. Use LINQ's
    /// <c>Queryable.Max</c> for the aggregate form.
    /// </summary>
    public static T Max<T>(params T[] values)
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
