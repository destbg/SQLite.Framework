namespace SQLite.Framework.Enums;

/// <summary>
/// SQLite's row-level conflict resolution clauses for INSERT statements.
/// Used by <c>AddOrUpdate</c> and <c>AddOrUpdateRange</c> to pick how SQLite reacts when an
/// inserted row conflicts with a UNIQUE or PRIMARY KEY constraint.
/// </summary>
public enum SQLiteConflict
{
    /// <summary>
    /// <c>INSERT OR REPLACE</c>. The default for <c>AddOrUpdate</c>. SQLite removes the
    /// conflicting row before inserting the new one.
    /// </summary>
    Replace,

    /// <summary>
    /// <c>INSERT OR IGNORE</c>. The conflicting row is left as is and the new row is dropped.
    /// </summary>
    Ignore,

    /// <summary>
    /// <c>INSERT OR ABORT</c>. The current statement is rolled back and an error is raised.
    /// </summary>
    Abort,

    /// <summary>
    /// <c>INSERT OR FAIL</c>. The current statement stops at the conflicting row. Rows already
    /// changed by the statement keep their changes.
    /// </summary>
    Fail,

    /// <summary>
    /// <c>INSERT OR ROLLBACK</c>. The whole transaction is rolled back and an error is raised.
    /// </summary>
    Rollback,
}
