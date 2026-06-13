namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A table whose rows were moved out so a referenced table could be rebuilt inside a transaction.
/// The table itself is left in place, so its schema, indexes and sequence are kept. Its triggers
/// are dropped first and recreated afterwards, so they do not fire while the rows are restored.
/// </summary>
internal class SavedTable
{
    public required string Name { get; init; }
    public required List<string> Triggers { get; init; }
    public required List<string> InsertableColumns { get; init; }
}
