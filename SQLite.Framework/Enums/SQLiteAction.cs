namespace SQLite.Framework.Enums;

/// <summary>
/// The CRUD action a <see cref="SQLiteActionHook" /> sees and can rewrite.
/// </summary>
public enum SQLiteAction
{
    /// <summary>
    /// Run the default INSERT.
    /// </summary>
    Add,

    /// <summary>
    /// Run the default UPDATE.
    /// </summary>
    Update,

    /// <summary>
    /// Run the default DELETE.
    /// </summary>
    Remove,

    /// <summary>
    /// Run an INSERT OR REPLACE.
    /// </summary>
    AddOrUpdate,

    /// <summary>
    /// Skip the row. No SQL is issued.
    /// </summary>
    Skip,
}
