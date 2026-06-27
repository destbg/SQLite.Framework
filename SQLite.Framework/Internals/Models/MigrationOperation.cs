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
    /// The mapping of the affected table, for reconcile, rename, and drop-column operations.
    /// </summary>
    public TableMapping? Mapping { get; init; }

    /// <summary>
    /// The values to write while reconciling, for a reconcile operation.
    /// </summary>
    public IReadOnlyList<(string Column, string ValueSql)> Sets { get; init; } = [];

    /// <summary>
    /// Whether a reconcile must rebuild the table rather than try in-place changes first.
    /// </summary>
    public bool Rebuild { get; init; }

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
    /// The raw SQL to run, for a raw-SQL operation.
    /// </summary>
    public string? Sql { get; init; }
}
