namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite functions that have no plain C# equivalent. These methods throw at
/// runtime. They are only valid inside a LINQ query, where the framework swaps them for the right
/// SQL. Examples are <c>RANDOM()</c>, <c>GLOB</c>, and <c>REGEXP</c>. FTS5 helpers live on
/// <see cref="SQLiteFTS5Functions" /> and window functions on <see cref="SQLiteWindowFunctions" />.
/// </summary>
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
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
    public static long UnixEpoch()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Parses <paramref name="when" /> as a date or time string and returns its Unix timestamp in
    /// seconds. Translates to SQLite's <c>unixepoch(when)</c>.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
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
    /// To negate, wrap the call with <c>!</c> (or compare to <c>false</c>). The framework emits
    /// <c>NOT (value BETWEEN low AND high)</c>, which SQLite treats the same as <c>NOT BETWEEN</c>.
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
    /// Returns the first non-null value in <paramref name="values" />, or <see langword="null" />
    /// when all are null. Maps to SQLite's <c>coalesce(v0, v1, ...)</c>. SQLite requires at
    /// least two arguments.
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
    /// Returns <paramref name="whenTrue" /> if <paramref name="condition" /> is true, otherwise
    /// <paramref name="whenFalse" />. Translates to SQLite's <c>iif(condition, whenTrue, whenFalse)</c>.
    /// SQLite implements <c>iif</c> as a shorthand for <c>CASE WHEN condition THEN whenTrue ELSE whenFalse END</c>.
    /// The C# ternary <c>?:</c> operator already translates to a <c>CASE</c> expression.
    /// Requires SQLite 3.32.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static T Iif<T>(bool condition, T whenTrue, T whenFalse)
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
    /// Decodes the hex string <paramref name="value" /> back into a BLOB. The inverse of
    /// <see cref="Hex" />. Translates to SQLite's <c>unhex(value)</c>. Requires SQLite 3.41.0
    /// or newer.
    /// </summary>
#if SQLITECIPHER
    [Obsolete("unhex is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios17.0")]
#endif
    public static byte[] Unhex(string value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Decodes the hex string <paramref name="value" />, ignoring any characters that appear in
    /// <paramref name="ignoreChars" />. Translates to SQLite's <c>unhex(value, ignoreChars)</c>.
    /// Requires SQLite 3.41.0 or newer.
    /// </summary>
#if SQLITECIPHER
    [Obsolete("unhex is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios17.0")]
#endif
    public static byte[] Unhex(string value, string ignoreChars)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Formats <paramref name="args" /> using the C-style <paramref name="format" /> string.
    /// Translates to SQLite's <c>format(format, ...)</c>, which is the modern alias for
    /// <c>printf</c>. Requires SQLite 3.38.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
    public static string Format(string format, params object[] args)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the Unicode code point of the first character of <paramref name="value" />.
    /// Translates to SQLite's <c>unicode(value)</c>.
    /// </summary>
    public static int Unicode(string value)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a string built from the given Unicode <paramref name="codePoints" />. Translates
    /// to SQLite's <c>char(c0, c1, ...)</c>.
    /// </summary>
    public static string Char(params int[] codePoints)
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
    /// Wraps <paramref name="value" /> in a <c>COLLATE</c> clause so the surrounding comparison or
    /// sort uses the given collation. <see cref="SQLiteCollation.Binary" /> emits no clause.
    /// </summary>
    public static string Collate(string value, SQLiteCollation collation)
    {
        throw new InvalidOperationException(OutsideQuery);
    }
}
