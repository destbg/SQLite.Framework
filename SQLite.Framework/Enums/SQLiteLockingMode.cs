namespace SQLite.Framework.Enums;

/// <summary>
/// Value for <c>PRAGMA locking_mode</c>. Controls how SQLite holds the database file lock
/// between transactions.
/// </summary>
public enum SQLiteLockingMode
{
    /// <summary>
    /// Default. SQLite acquires and releases locks per transaction so other processes can
    /// share the file.
    /// </summary>
    Normal,

    /// <summary>
    /// SQLite holds the file lock for the lifetime of the connection. Faster for single-process
    /// access because lock state does not roundtrip to the OS, but other processes cannot
    /// access the file at all.
    /// </summary>
    Exclusive,
}
