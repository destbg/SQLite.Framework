namespace SQLite.Framework.Enums;

/// <summary>
/// Value for <c>PRAGMA encoding</c>. Only takes effect when applied before any table is
/// created in a new database. SQLite stores the chosen encoding in the file header.
/// </summary>
public enum SQLiteEncoding
{
    /// <summary>
    /// Default. UTF-8 bytes on disk.
    /// </summary>
    Utf8,

    /// <summary>
    /// UTF-16 in the native byte order of the machine that creates the file.
    /// </summary>
    Utf16,

    /// <summary>
    /// UTF-16 little-endian.
    /// </summary>
    Utf16le,

    /// <summary>
    /// UTF-16 big-endian.
    /// </summary>
    Utf16be,
}
