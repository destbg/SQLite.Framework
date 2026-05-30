namespace SQLite.Framework.Enums;

/// <summary>
/// The row change that fires a trigger. Used by
/// <see cref="SQLiteSchema.CreateTrigger{T}(string, SQLiteTriggerTiming, SQLiteTriggerEvent, string, string?, bool)" />.
/// </summary>
public enum SQLiteTriggerEvent
{
    /// <summary>
    /// The trigger fires on <c>INSERT</c>.
    /// </summary>
    Insert,

    /// <summary>
    /// The trigger fires on <c>UPDATE</c>.
    /// </summary>
    Update,

    /// <summary>
    /// The trigger fires on <c>DELETE</c>.
    /// </summary>
    Delete,
}
