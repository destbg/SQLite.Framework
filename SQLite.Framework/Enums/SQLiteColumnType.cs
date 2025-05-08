namespace SQLite.Framework.Enums;

/// <summary>
/// Represents the SQLite column types.
/// </summary>
public enum SQLiteColumnType
{
    /// <summary>
    /// Represents an integer column type.
    /// </summary>
    Integer = 1,

    /// <summary>
    /// Represents a real column type.
    /// </summary>
    Real = 2,

    /// <summary>
    /// Represents a text column type.
    /// </summary>
    Text = 3,

    /// <summary>
    /// Represents a blob column type.
    /// </summary>
    Blob = 4,

    /// <summary>
    /// Represents a null column type.
    /// </summary>
    Null = 5,
}