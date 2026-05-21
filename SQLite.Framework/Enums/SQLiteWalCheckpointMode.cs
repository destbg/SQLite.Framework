namespace SQLite.Framework.Enums;

/// <summary>
/// Mode argument for <c>PRAGMA wal_checkpoint(MODE)</c>. Controls how aggressively SQLite
/// transfers WAL frames back to the main database file.
/// </summary>
public enum SQLiteWalCheckpointMode
{
    /// <summary>
    /// Default. Checkpoint as many frames as possible without waiting for readers. Returns when
    /// either the WAL is fully checkpointed or a reader blocks progress.
    /// </summary>
    Passive,

    /// <summary>
    /// Wait for readers to finish, then checkpoint the entire WAL. Other connections cannot
    /// start new transactions until the checkpoint completes.
    /// </summary>
    Full,

    /// <summary>
    /// Same as <see cref="Full" /> but also restarts the WAL so the next write starts a fresh
    /// log.
    /// </summary>
    Restart,

    /// <summary>
    /// Same as <see cref="Restart" /> but also truncates the WAL file to zero bytes when it is
    /// safe to do so.
    /// </summary>
    Truncate,
}
