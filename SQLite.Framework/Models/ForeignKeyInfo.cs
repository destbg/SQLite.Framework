namespace SQLite.Framework.Models;

/// <summary>
/// Resolved foreign key metadata. Lives on a <see cref="TableColumn" /> for single-column keys
/// and on <see cref="TableMapping" /> for composite keys.
/// </summary>
public class ForeignKeyInfo
{
    private readonly Lazy<(string Table, IReadOnlyList<string> Columns)> target;

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
        target = new Lazy<(string, IReadOnlyList<string>)>(() => (targetTable, targetColumns));
        OnDelete = onDelete;
        OnUpdate = onUpdate;
        Deferred = deferred;
    }

    /// <summary>
    /// Initializes a foreign key whose target names resolve on first use. The fluent builder uses
    /// this so a later rename of the parent's table or columns still reaches the key.
    /// </summary>
    internal ForeignKeyInfo(IReadOnlyList<string> columns, Func<(string Table, IReadOnlyList<string> Columns)> resolveTarget, SQLiteForeignKeyAction onDelete, SQLiteForeignKeyAction onUpdate, bool deferred)
    {
        Columns = columns;
        target = new Lazy<(string, IReadOnlyList<string>)>(resolveTarget);
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
    public string TargetTable => target.Value.Table;

    /// <summary>
    /// Column names on the target table, in the same order as <see cref="Columns" />.
    /// </summary>
    public IReadOnlyList<string> TargetColumns => target.Value.Columns;

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
