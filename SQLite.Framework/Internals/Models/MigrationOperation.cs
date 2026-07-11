namespace SQLite.Framework.Internals.Models;

/// <summary>
/// One unit of work declared on a migration step. The runner reads these to build a plan and to
/// apply a migration. Only the fields that match <see cref="Kind" /> are set.
/// </summary>
internal sealed class MigrationOperation
{
    /// <summary>
    /// The kind of work this operation performs.
    /// </summary>
    public required MigrationOperationKind Kind { get; init; }

    /// <summary>
    /// A short description shown in a <see cref="SQLiteMigrationPlan" />.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The version this operation was declared at. The runner stamps it while building a step.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The mapping of the affected table, for reconcile, rename and drop-column operations.
    /// </summary>
    public TableMapping? Mapping { get; init; }

    /// <summary>
    /// The values to write while reconciling, for a reconcile operation.
    /// </summary>
    public IReadOnlyList<MigrationSetValue> Sets { get; init; } = [];

    /// <summary>
    /// Whether a reconcile must rebuild the table rather than try in-place changes first.
    /// </summary>
    public bool Rebuild { get; init; }

    /// <summary>
    /// Whether a create-table operation drops an existing table with the same name first. The
    /// runner sets this when a pending drop of the table is declared before the create, so a
    /// collapsed run ends in the same state as running the versions one at a time.
    /// </summary>
    public bool DropTableFirst { get; set; }

    /// <summary>
    /// The mapping to recreate the table from right after a drop-table operation runs. The runner
    /// sets this when a pending create of the same table is declared after the drop, so raw SQL
    /// and callback steps between the two see the drop at its version, like a stepwise run does.
    /// </summary>
    public TableMapping? RecreateMapping { get; set; }

    /// <summary>
    /// The current SQLite table name, for a rename-table operation. The table is renamed to the
    /// name of <see cref="Mapping" />.
    /// </summary>
    public string? FromTable { get; init; }

    /// <summary>
    /// The current SQLite column name, for a rename operation.
    /// </summary>
    public string? FromColumn { get; init; }

    /// <summary>
    /// The new SQLite column name, for a rename operation.
    /// </summary>
    public string? ToColumn { get; init; }

    /// <summary>
    /// The SQLite column name to drop, for a drop-column operation.
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>
    /// The SQLite table name to drop, for a drop-table operation.
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Runs the data work and returns the affected row count, for insert, update, delete, view
    /// and full-text-search rebuild operations.
    /// </summary>
    public Func<SQLiteDatabase, int>? Execute { get; init; }

    /// <summary>
    /// The raw SQL to run, for a raw-SQL operation.
    /// </summary>
    public string? Sql { get; init; }

    /// <summary>
    /// The parameters bound to <see cref="Sql" />, for a raw-SQL operation.
    /// </summary>
    public SQLiteParameter[] SqlParameters { get; init; } = [];

    /// <summary>
    /// The callback to run, for a run or run-before operation.
    /// </summary>
    public Action<SQLiteMigrationContext>? Callback { get; init; }

    /// <summary>
    /// The callback to await, for an async run or run-before operation.
    /// </summary>
    public Func<SQLiteMigrationContext, Task>? AsyncCallback { get; init; }
}
