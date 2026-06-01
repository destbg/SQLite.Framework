namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how char values are stored in and read from the database.
/// </summary>
public enum CharStorageMode
{
    /// <summary>
    /// Stored as a single-character TEXT string. This is the default.
    /// A lone UTF-16 surrogate cannot be stored this way and reads back as the replacement character.
    /// </summary>
    Text,

    /// <summary>
    /// Stored as an INTEGER holding the UTF-16 code unit. This round-trips every char value exactly,
    /// including lone surrogates.
    /// </summary>
    Integer,
}
