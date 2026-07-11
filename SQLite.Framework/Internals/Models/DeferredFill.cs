namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A data-phase UPDATE fill produced while reconciling a table, scheduled to run at its declaring
/// version between the data operations, so it lands after the data work of earlier versions.
/// </summary>
internal sealed class DeferredFill
{
    /// <summary>
    /// The version the fill was declared at.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// The mapping of the table the fill updates.
    /// </summary>
    public required TableMapping Mapping { get; init; }

    /// <summary>
    /// The column values the fill writes.
    /// </summary>
    public required IReadOnlyList<MigrationSetValue> Sets { get; init; }
}
