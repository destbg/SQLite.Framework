namespace SQLite.Framework.Enums;

/// <summary>
/// Behavior of <c>PRAGMA auto_vacuum</c>. Auto-vacuum controls whether SQLite reclaims free
/// pages automatically. The setting only takes effect when applied before the database is
/// first written.
/// </summary>
public enum SQLiteAutoVacuumMode
{
    /// <summary>
    /// Free pages stay in the file until <c>VACUUM</c> is run manually. SQLite's default.
    /// </summary>
    None = 0,

    /// <summary>
    /// SQLite truncates free pages from the end of the file at every commit. Has overhead per
    /// transaction.
    /// </summary>
    Full = 1,

    /// <summary>
    /// Same as <see cref="Full" /> but only when <c>PRAGMA incremental_vacuum</c> is invoked
    /// explicitly. Lets the app pick the moment to pay the cost.
    /// </summary>
    Incremental = 2,
}
