namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how enum values are stored in and read from the database.
/// </summary>
public enum EnumStorageMode
{
    /// <summary>
    /// Stored as an INTEGER. This is the default.
    /// </summary>
    Integer,

    /// <summary>
    /// Stored as a TEXT string containing the member name, for example "Active".
    /// </summary>
    Text,
}
