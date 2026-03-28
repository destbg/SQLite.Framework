namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how DateOnly values are stored in and read from the database.
/// </summary>
public enum DateOnlyStorageMode
{
    /// <summary>
    /// Stored as an INTEGER tick count. This is the default.
    /// </summary>
    Integer,

    /// <summary>
    /// Stored as a formatted TEXT date string, for example "2000-02-03".
    /// LINQ property access such as Year, Month, and Day is not supported in this mode.
    /// </summary>
    Text,
}
