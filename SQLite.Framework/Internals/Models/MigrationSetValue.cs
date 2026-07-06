namespace SQLite.Framework.Internals.Models;

/// <summary>
/// One column value declared with <c>Set</c> while reconciling a table, together with the
/// database columns its value expression reads. The reads and the live table shape decide where
/// the fill runs. A fill that reads a column outside the current model, targets a column the
/// live table misses or turns a nullable column into <c>NOT NULL</c> runs inside the
/// schema-phase rebuild, so it sees the old row exactly once. Every other fill runs as an
/// UPDATE in the data phase, in version order.
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
}
