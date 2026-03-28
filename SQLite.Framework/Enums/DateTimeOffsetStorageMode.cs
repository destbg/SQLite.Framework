namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how DateTimeOffset values are stored in and read from the database.
/// </summary>
public enum DateTimeOffsetStorageMode
{
    /// <summary>
    /// Stored as an INTEGER tick count. This is the default.
    /// The offset needs to be stored in a separate column.
    /// </summary>
    Ticks,

    /// <summary>
    /// Stored as an INTEGER UTC tick count.
    /// The original offset is not preserved.
    /// </summary>
    UtcTicks,

    /// <summary>
    /// Stored as a formatted TEXT date string, for example "2000-02-03 04:05:06 +05:30".
    /// LINQ property access such as Year, Month, and Day is not supported in this mode.
    /// </summary>
    TextFormatted,
}
