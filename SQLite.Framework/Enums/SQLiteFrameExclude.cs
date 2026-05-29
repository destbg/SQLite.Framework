namespace SQLite.Framework.Enums;

/// <summary>
/// Controls the <c>EXCLUDE</c> part of a window frame. It decides which rows near the current
/// row are left out of the frame the window function sees. Added in SQLite 3.28.0.
/// </summary>
public enum SQLiteFrameExclude
{
    /// <summary>
    /// Default. No rows are excluded, which is what SQLite does when the clause is omitted, so
    /// nothing is emitted.
    /// </summary>
    NoOthers,

    /// <summary>
    /// Leaves out the current row. Emits <c>EXCLUDE CURRENT ROW</c>.
    /// </summary>
    CurrentRow,

    /// <summary>
    /// Leaves out the current row together with its peers (rows that share the same <c>ORDER BY</c>
    /// value). Emits <c>EXCLUDE GROUP</c>.
    /// </summary>
    Group,

    /// <summary>
    /// Leaves out the peers of the current row but keeps the current row itself. Emits
    /// <c>EXCLUDE TIES</c>.
    /// </summary>
    Ties,
}
