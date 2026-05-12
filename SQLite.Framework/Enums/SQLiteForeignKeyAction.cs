namespace SQLite.Framework.Enums;

/// <summary>
/// SQLite referential action emitted after <c>ON DELETE</c> or <c>ON UPDATE</c> in a foreign key
/// constraint. See <see href="https://www.sqlite.org/foreignkeys.html#fk_actions">SQLite docs</see>
/// for the full semantics.
/// </summary>
public enum SQLiteForeignKeyAction
{
    /// <summary>
    /// SQLite's default. The constraint check is deferred to the end of the statement and an
    /// error is raised if a child row would be orphaned.
    /// </summary>
    NoAction,

    /// <summary>
    /// The parent change is rejected immediately if any child row references it.
    /// </summary>
    Restrict,

    /// <summary>
    /// Child columns are set to <see langword="null" />. The child columns must be nullable.
    /// </summary>
    SetNull,

    /// <summary>
    /// Child columns are set to their declared <c>DEFAULT</c> value.
    /// </summary>
    SetDefault,

    /// <summary>
    /// On delete, child rows are deleted too. On update, child columns are updated to the new
    /// parent value.
    /// </summary>
    Cascade,
}
