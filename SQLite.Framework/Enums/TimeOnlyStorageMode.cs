namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how TimeOnly values are stored in and read from the database.
/// </summary>
public enum TimeOnlyStorageMode
{
    /// <summary>
    /// Stored as an INTEGER tick count. This is the default.
    /// </summary>
    Integer,

    /// <summary>
    /// Stored as a formatted TEXT time string, for example "04:05:06".
    /// LINQ property access such as Hour, Minute, and Second is not supported in this mode.
    /// </summary>
    Text,
}
