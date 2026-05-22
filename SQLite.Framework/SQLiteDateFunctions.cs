namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite's date and time SQL functions. These methods throw at runtime and
/// are only valid inside a LINQ query, where the framework swaps them for the right SQL.
/// </summary>
public static class SQLiteDateFunctions
{
    private const string OutsideQuery = "SQLiteDateFunctions methods can only be used inside a LINQ query.";

    /// <summary>
    /// Translates to SQLite's <c>date()</c>, which returns the current date as
    /// <c>YYYY-MM-DD</c>.
    /// </summary>
    public static string Date()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>date(when, modifiers...)</c>.
    /// </summary>
    public static string Date(string when, params string[] modifiers)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>date(when, modifiers...)</c> with a numeric time value. SQLite
    /// reads the number as a Julian day. Add <c>"unixepoch"</c> as the first modifier to treat
    /// it as seconds since the unix epoch.
    /// </summary>
    public static string Date<T>(T when, params string[] modifiers)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>time()</c>, which returns the current time as
    /// <c>HH:MM:SS</c>.
    /// </summary>
    public static string Time()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>time(when, modifiers...)</c>.
    /// </summary>
    public static string Time(string when, params string[] modifiers)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>time(when, modifiers...)</c> with a numeric time value. SQLite
    /// reads the number as a Julian day. Add <c>"unixepoch"</c> as the first modifier to treat
    /// it as seconds since the unix epoch.
    /// </summary>
    public static string Time<T>(T when, params string[] modifiers)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>datetime()</c>, which returns the current local datetime as
    /// <c>YYYY-MM-DD HH:MM:SS</c>.
    /// </summary>
    public static string Datetime()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>datetime(when, modifiers...)</c>.
    /// </summary>
    public static string Datetime(string when, params string[] modifiers)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>datetime(when, modifiers...)</c> with a numeric time value.
    /// SQLite reads the number as a Julian day. Add <c>"unixepoch"</c> as the first modifier to
    /// treat it as seconds since the unix epoch.
    /// </summary>
    public static string Datetime<T>(T when, params string[] modifiers)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>julianday()</c>, which returns the current Julian day number.
    /// </summary>
    public static double JulianDay()
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>julianday(when, modifiers...)</c>.
    /// </summary>
    public static double JulianDay(string when, params string[] modifiers)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>julianday(when, modifiers...)</c> with a numeric time value.
    /// SQLite reads the number as a Julian day. Add <c>"unixepoch"</c> as the first modifier to
    /// treat it as seconds since the unix epoch.
    /// </summary>
    public static double JulianDay<T>(T when, params string[] modifiers)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>strftime(format, when, modifiers...)</c>. Formats the time
    /// value using SQLite's strftime conversion specifiers (<c>%Y</c>, <c>%m</c>, <c>%d</c>,
    /// <c>%H</c>, <c>%M</c>, <c>%S</c>, <c>%j</c>, <c>%w</c>, <c>%W</c>, <c>%s</c>, etc.).
    /// </summary>
    public static string Strftime(string format, string when, params string[] modifiers)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>strftime(format, when, modifiers...)</c> with a numeric time
    /// value. SQLite reads the number as a Julian day. Add <c>"unixepoch"</c> as the first
    /// modifier to treat it as seconds since the unix epoch.
    /// </summary>
    public static string Strftime<T>(string format, T when, params string[] modifiers)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>timediff(when1, when2)</c>. Returns the difference between two
    /// time values as a string. Requires SQLite 3.43.0 or newer.
    /// </summary>
#if SQLITECIPHER
    [Obsolete("timediff is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android35.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios17.0")]
#endif
    public static string Timediff(string when1, string when2)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Translates to SQLite's <c>timediff(when1, when2)</c> with numeric time values. SQLite
    /// reads each number as a Julian day. Requires SQLite 3.43.0 or newer.
    /// </summary>
#if SQLITECIPHER
    [Obsolete("timediff is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android35.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios17.0")]
#endif
    public static string Timediff<T>(T when1, T when2)
        where T : INumber<T>
    {
        throw new InvalidOperationException(OutsideQuery);
    }
}
