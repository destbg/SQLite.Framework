namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how the framework keeps an external-content FTS5 table in sync with its source table.
/// </summary>
public enum FtsAutoSync
{
    /// <summary>
    /// The framework does nothing extra. You insert, update, and delete rows in the FTS table yourself.
    /// This is the default.
    /// </summary>
    Manual,

    /// <summary>
    /// On <c>CreateTable</c>, the framework also creates <c>AFTER INSERT</c>, <c>AFTER UPDATE</c>,
    /// and <c>AFTER DELETE</c> triggers on the source table that mirror writes into the FTS table.
    /// </summary>
    Triggers,
}
