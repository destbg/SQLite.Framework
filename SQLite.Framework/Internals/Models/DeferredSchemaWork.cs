namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A schema-phase action moved into the data phase. The runner defers a rename or a reconcile of
/// a table that does not exist yet when raw SQL or a callback of an earlier version is pending,
/// since that step may create the table, like it did when the versions ran one at a time.
/// </summary>
internal sealed class DeferredSchemaWork
{
    /// <summary>
    /// The version the action was declared at.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Orders actions declared at the same version the way the schema phase orders them, table
    /// renames first, column renames next, reconciles last.
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Runs the action and returns the statement count together with the fills it produced.
    /// </summary>
    public required Func<(int Count, List<DeferredFill> Fills)> Apply { get; init; }
}
