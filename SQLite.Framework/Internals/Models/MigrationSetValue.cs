namespace SQLite.Framework.Internals.Models;

/// <summary>
/// One column value declared with <c>Set</c> while reconciling a table, together with the
/// database columns its value expression reads. The reads and the live table shape decide where
/// the fill runs. A fill that reads a column outside the current model or that reads its own
/// column runs inside the schema-phase rebuild, so it sees the old row exactly once. Every other
/// fill runs as an UPDATE in the data phase, in version order, so it lands after the data work
/// of earlier versions. When such a fill is also needed during the rebuild, to satisfy a new or
/// newly NOT NULL column, the rebuild applies it only where the column is NULL and the
/// data-phase UPDATE writes the final value.
/// </summary>
internal sealed class MigrationSetValue
{
    /// <summary>
    /// The SQLite name of the column the fill writes.
    /// </summary>
    public required string Column { get; init; }

    /// <summary>
    /// The SQL fragment that produces the value, with literals inlined.
    /// </summary>
    public required string ValueSql { get; init; }

    /// <summary>
    /// The SQLite names of the columns the value expression reads.
    /// </summary>
    public IReadOnlyList<string> ReadColumns { get; init; } = [];

    /// <summary>
    /// When <see langword="true" />, the fill always runs inside the schema-phase rebuild instead
    /// of a data-phase UPDATE, so it rewrites the column while the table is rebuilt. <c>Reconvert</c>
    /// sets this so a column whose converter changed its stored form is re-encoded during the copy,
    /// which a STRICT table needs because it will not store the old form in the new column.
    /// </summary>
    public bool RunInRebuild { get; init; }
}
