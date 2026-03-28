namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how TimeSpan values are stored in and read from the database.
/// </summary>
public enum TimeSpanStorageMode
{
    /// <summary>
    /// Stored as an INTEGER tick count. This is the default.
    /// </summary>
    Integer,

    /// <summary>
    /// Stored as a TEXT string in the standard constant format, for example "2.03:04:05.0060070".
    /// LINQ property access such as Days, Hours, and TotalDays is not supported in this mode.
    /// </summary>
    Text,
}
