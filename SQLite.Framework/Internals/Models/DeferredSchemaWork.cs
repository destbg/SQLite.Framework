namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A schema-phase action moved into the data phase. The runner defers a rename, a create or a
/// reconcile when running it early would not match a stepwise run. That happens when raw SQL or
/// a callback of an earlier version may create the table, when a pending drop of an earlier
/// version targets the table or when the reconcile writes values that an earlier data step must
/// see or produce first.
/// </summary>
internal sealed class DeferredSchemaWork
{
    /// <summary>
    /// The version the action was declared at.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Orders actions declared at the same version the way the schema phase orders them, table
    /// renames first, column renames next, creates next, reconciles last.
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Runs the action and returns the statement count together with the fills it produced.
    /// </summary>
    public required Func<(int Count, List<DeferredFill> Fills)> Apply { get; init; }
}
