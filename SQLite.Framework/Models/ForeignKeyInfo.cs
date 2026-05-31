namespace SQLite.Framework.Models;

/// <summary>
/// Resolved foreign key metadata. Lives on a <see cref="TableColumn" /> for single-column keys
/// and on <see cref="TableMapping" /> for composite keys.
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>
    /// Initializes a new foreign key.
    /// </summary>
    public ForeignKeyInfo(IReadOnlyList<string> columns, string targetTable, IReadOnlyList<string> targetColumns, SQLiteForeignKeyAction onDelete, SQLiteForeignKeyAction onUpdate, bool deferred)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(targetTable);
        ArgumentNullException.ThrowIfNull(targetColumns);
        if (columns.Count == 0)
        {
            throw new ArgumentException("Foreign key must reference at least one column.", nameof(columns));
        }
        if (columns.Count != targetColumns.Count)
        {
            throw new ArgumentException("Local and target column counts must match.", nameof(targetColumns));
        }

        Columns = columns;
        TargetTable = targetTable;
        TargetColumns = targetColumns;
        OnDelete = onDelete;
        OnUpdate = onUpdate;
        Deferred = deferred;
    }

    /// <summary>
    /// Column names on the local table participating in the foreign key, in declaration order.
    /// </summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>
    /// The referenced table's name.
    /// </summary>
    public string TargetTable { get; }

    /// <summary>
    /// Column names on the target table, in the same order as <see cref="Columns" />.
    /// </summary>
    public IReadOnlyList<string> TargetColumns { get; }

    /// <summary>
    /// Action to take when the parent row is deleted.
    /// </summary>
    public SQLiteForeignKeyAction OnDelete { get; }

    /// <summary>
    /// Action to take when the parent row's referenced column is updated.
    /// </summary>
    public SQLiteForeignKeyAction OnUpdate { get; }

    /// <summary>
    /// <see langword="true" /> when the constraint should emit <c>DEFERRABLE INITIALLY DEFERRED</c>.
    /// </summary>
    public bool Deferred { get; }
}
