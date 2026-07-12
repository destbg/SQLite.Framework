namespace SQLite.Framework.Internals.Enums;

/// <summary>
/// The storage affinity class SQLite assigns to a column from its declared type name.
/// </summary>
internal enum SQLiteTypeAffinity
{
    Integer,
    Text,
    Blob,
    Real,
    Numeric,
}
