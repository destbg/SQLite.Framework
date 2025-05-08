namespace SQLite.Framework.Enums;

/// <summary>
/// Represents the result codes returned by SQLite operations.
/// </summary>
public enum SQLiteResult
{
    /// <summary>Operation completed successfully.</summary>
    OK = 0,

    /// <summary>Generic error occurred.</summary>
    Error = 1,

    /// <summary>Internal logic error in SQLite.</summary>
    Internal = 2,

    /// <summary>Access permission denied.</summary>
    Perm = 3,

    /// <summary>Operation was aborted.</summary>
    Abort = 4,

    /// <summary>The database file is locked.</summary>
    Busy = 5,

    /// <summary>A table in the database is locked.</summary>
    Locked = 6,

    /// <summary>A malloc() failed.</summary>
    NoMem = 7,

    /// <summary>Attempt to write a readonly database.</summary>
    ReadOnly = 8,

    /// <summary>Operation was interrupted.</summary>
    Interrupt = 9,

    /// <summary>Some kind of disk I/O error occurred.</summary>
    IOError = 10,

    /// <summary>The database disk image is malformed.</summary>
    Corrupt = 11,

    /// <summary>Table or record not found.</summary>
    NotFound = 12,

    /// <summary>Insertion failed because database is full.</summary>
    Full = 13,

    /// <summary>Unable to open the database file.</summary>
    CannotOpen = 14,

    /// <summary>A locking protocol error occurred.</summary>
    LockErr = 15,

    /// <summary>Database is empty.</summary>
    Empty = 16,

    /// <summary>The database schema changed.</summary>
    SchemaChngd = 17,

    /// <summary>String or BLOB exceeds size limit.</summary>
    TooBig = 18,

    /// <summary>Abort due to constraint violation.</summary>
    Constraint = 19,

    /// <summary>Data type mismatch.</summary>
    Mismatch = 20,

    /// <summary>Library used incorrectly.</summary>
    Misuse = 21,

    /// <summary>Uses OS features not supported on host.</summary>
    NotImplementedLFS = 22,

    /// <summary>Authorization denied.</summary>
    AccessDenied = 23,

    /// <summary>Auxiliary database format error.</summary>
    Format = 24,

    /// <summary>2nd parameter to sqlite3_bind out of range.</summary>
    Range = 25,

    /// <summary>File opened that is not a database file.</summary>
    NonDBFile = 26,

    /// <summary>Notifications from sqlite3_log().</summary>
    Notice = 27,

    /// <summary>Warnings from sqlite3_log().</summary>
    Warning = 28,

    /// <summary>sqlite3_step() has another row ready.</summary>
    Row = 100,

    /// <summary>sqlite3_step() has finished executing.</summary>
    Done = 101
}
