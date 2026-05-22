namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Renders a SQLite version number (the format returned by
/// <c>sqlite3_libversion_number()</c>, e.g. <c>3032000</c>) as a human-readable
/// <c>major.minor.patch</c> string.
/// </summary>
internal static class SQLiteVersionFormatter
{
    public static string Format(int versionNumber)
    {
        int major = versionNumber / 1_000_000;
        int minor = versionNumber / 1_000 % 1_000;
        int patch = versionNumber % 1_000;
        return $"{major}.{minor}.{patch}";
    }
}
