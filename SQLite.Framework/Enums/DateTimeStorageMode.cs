namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how DateTime values are stored in and read from the database.
/// </summary>
public enum DateTimeStorageMode
{
    /// <summary>
    /// Stored as an INTEGER tick count. This is the default.
    /// </summary>
    Integer,

    /// <summary>
    /// Stored as a TEXT string containing the raw tick count, for example "638000000000000000".
    /// This is only here to provide support for those migrating from other sqlite libraries.
    /// </summary>
    TextTicks,

    /// <summary>
    /// Stored as a formatted TEXT date string, for example "2000-02-03 04:05:06".
    /// LINQ property access such as Year, Month, and Day is not supported in this mode.
    /// </summary>
    TextFormatted,
}
