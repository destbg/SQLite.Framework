namespace SQLite.Framework.Enums;

/// <summary>
/// Value for <c>PRAGMA journal_mode</c>. Controls how SQLite keeps the rollback journal that lets
/// it undo a transaction. These are the only modes SQLite accepts.
/// </summary>
public enum SQLiteJournalMode
{
    /// <summary>
    /// Default. The rollback journal is deleted at the end of each transaction.
    /// </summary>
    Delete,

    /// <summary>
    /// The rollback journal is truncated to zero length instead of deleted, which is faster on
    /// most systems because the file does not have to be removed.
    /// </summary>
    Truncate,

    /// <summary>
    /// The rollback journal is left in place but its header is overwritten with zeros, which
    /// avoids deleting or truncating the file.
    /// </summary>
    Persist,

    /// <summary>
    /// The rollback journal is kept in memory rather than on disk. Faster, but a crash mid
    /// transaction can corrupt the database.
    /// </summary>
    Memory,

    /// <summary>
    /// Write-ahead logging. Readers do not block the writer and the writer does not block readers.
    /// Persists across connections and requires a file-backed database.
    /// </summary>
    Wal,

    /// <summary>
    /// No rollback journal is kept. The <c>ROLLBACK</c> command stops working and a crash mid
    /// transaction can corrupt the database.
    /// </summary>
    Off,
}
