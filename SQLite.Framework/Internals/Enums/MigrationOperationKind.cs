namespace SQLite.Framework.Internals.Enums;

/// <summary>
/// The kind of work a single migration step performs. Used to group steps into the phases the
/// runner applies them in.
/// </summary>
internal enum MigrationOperationKind
{
    CreateTable,
    Reconcile,
    RenameTable,
    RenameColumn,
    DropColumn,
    DropTable,
    RawSql,
    InsertRows,
    UpdateRows,
    DeleteRows,
    CreateView,
    DropView,
    RebuildFullTextSearch,
    Run,
    RunBefore,
}
