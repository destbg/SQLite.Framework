namespace SQLite.Framework;

/// <summary>
/// The information passed to the callback declared with
/// <see cref="SQLiteMigrationRunner.Progress" />, once for each operation of a run, right before
/// the operation is applied.
/// </summary>
public sealed class SQLiteMigrationProgress
{
    internal SQLiteMigrationProgress(int version, string description, int index, int count)
    {
        Version = version;
        Description = description;
        Index = index;
        Count = count;
    }

    /// <summary>
    /// The version that declared the operation.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// A short description of the operation, the same text a <see cref="SQLiteMigrationPlan" />
    /// shows.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The one-based position of this operation in the run, in the order the runner applies the
    /// operations.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The number of operations in the run.
    /// </summary>
    public int Count { get; }
}
