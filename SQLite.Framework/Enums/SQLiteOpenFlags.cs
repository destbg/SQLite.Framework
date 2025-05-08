namespace SQLite.Framework.Enums;

/// <summary>
/// Flags that specify the open mode for a SQLite database.
/// </summary>
[Flags]
public enum SQLiteOpenFlags
{
    /// <summary>
    /// Opens the database in read-only mode.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Opens the database in read-write mode.
    /// </summary>
    ReadWrite = 2,

    /// <summary>
    /// Creates the database file if it does not exist.
    /// </summary>
    Create = 4,

    /// <summary>
    /// Interprets the filename as a URI.
    /// </summary>
    Uri = 0x40,

    /// <summary>
    /// Opens the database in memory.
    /// </summary>
    Memory = 0x80,

    /// <summary>
    /// Disables mutexes for multithreading.
    /// </summary>
    NoMutex = 0x8000,

    /// <summary>
    /// Enables mutexes for multithreading.
    /// </summary>
    FullMutex = 0x10000,

    /// <summary>
    /// Enables shared cache mode.
    /// </summary>
    SharedCache = 0x20000,

    /// <summary>
    /// Enables private cache mode.
    /// </summary>
    PrivateCache = 0x40000,

    /// <summary>
    /// Ensures data protection is complete.
    /// </summary>
    ProtectionComplete = 0x00100000,

    /// <summary>
    /// Ensures data protection is complete unless the database is open.
    /// </summary>
    ProtectionCompleteUnlessOpen = 0x00200000,

    /// <summary>
    /// Ensures data protection is complete until the first user authentication.
    /// </summary>
    ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,

    /// <summary>
    /// Disables data protection.
    /// </summary>
    ProtectionNone = 0x00400000
}
