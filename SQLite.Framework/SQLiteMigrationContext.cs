namespace SQLite.Framework;

/// <summary>
/// The information passed to a callback declared with
/// <see cref="SQLiteMigrationStep.Run(Action{SQLiteMigrationContext})" />,
/// <see cref="SQLiteMigrationStep.RunAsync(Func{SQLiteMigrationContext, Task})" /> or their
/// run-before counterparts.
/// </summary>
public sealed class SQLiteMigrationContext
{
    internal SQLiteMigrationContext(SQLiteDatabase database, int fromVersion, int targetVersion, CancellationToken cancellationToken)
    {
        Database = database;
        FromVersion = fromVersion;
        TargetVersion = targetVersion;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The database the migration runs on. Use the sync methods inside <c>Run</c> and
    /// <c>RunBefore</c> and the async methods inside <c>RunAsync</c> and <c>RunBeforeAsync</c>.
    /// The run holds a re-entrant connection lock, so the calls do not fight the migration for it.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The version the database recorded when the run started.
    /// </summary>
    public int FromVersion { get; }

    /// <summary>
    /// The highest declared version, which the run moves the database to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// The token passed to <c>MigrateAsync</c>. Pass it to the async database methods inside an
    /// awaited callback. <see cref="CancellationToken.None" /> when the run started with
    /// <see cref="SQLiteMigrationRunner.Migrate" />.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
