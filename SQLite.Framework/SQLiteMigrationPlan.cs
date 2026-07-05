namespace SQLite.Framework;

/// <summary>
/// A read-only view of what a <see cref="SQLiteMigrationRunner" /> would do. Built by
/// <see cref="SQLiteMigrationRunner.Plan" /> without changing the database. Use it to see whether a
/// migration is needed and what it would run before you apply it.
/// </summary>
public sealed class SQLiteMigrationPlan
{
    internal SQLiteMigrationPlan(int currentVersion, int targetVersion, IReadOnlyList<string> operations)
    {
        CurrentVersion = currentVersion;
        TargetVersion = targetVersion;
        Operations = operations;
    }

    /// <summary>
    /// The version recorded in the database now, read from <c>PRAGMA user_version</c>.
    /// </summary>
    public int CurrentVersion { get; }

    /// <summary>
    /// The highest declared version, which the runner migrates to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// True when the database is exactly at the target version, so there is nothing to do.
    /// </summary>
    public bool IsUpToDate => CurrentVersion == TargetVersion;

    /// <summary>
    /// True when the database records a version above the highest declared one, which means a
    /// newer app version created it. The schema may not match this build of the app.
    /// <see cref="SQLiteMigrationRunner.Migrate" /> throws in this state, so check this flag to
    /// give the user a better message.
    /// </summary>
    public bool DatabaseIsNewer => CurrentVersion > TargetVersion;

    /// <summary>
    /// One short description for each operation that would run, in the order the versions are
    /// declared. Empty when the database is up to date.
    /// </summary>
    public IReadOnlyList<string> Operations { get; }
}
