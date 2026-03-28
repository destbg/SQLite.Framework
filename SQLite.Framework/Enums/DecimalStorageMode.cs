namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how decimal values are stored in and read from the database.
/// </summary>
public enum DecimalStorageMode
{
    /// <summary>
    /// Stored as a REAL (double precision floating point). This is the default.
    /// </summary>
    Real,

    /// <summary>
    /// Stored as a TEXT string to preserve full precision.
    /// When used in SQL operations, the value is cast to REAL automatically.
    /// </summary>
    Text,
}
