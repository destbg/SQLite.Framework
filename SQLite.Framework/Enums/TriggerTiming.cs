namespace SQLite.Framework.Enums;

/// <summary>
/// When a trigger fires relative to the row change. Used by <see cref="SQLiteSchema.CreateTrigger{T}" />.
/// </summary>
public enum TriggerTiming
{
    /// <summary>
    /// The trigger runs before the row change.
    /// </summary>
    Before,

    /// <summary>
    /// The trigger runs after the row change.
    /// </summary>
    After,

    /// <summary>
    /// The trigger replaces the row change. Only valid on views.
    /// </summary>
    InsteadOf,
}
