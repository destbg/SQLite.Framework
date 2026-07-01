namespace SQLite.Framework.Enums;

/// <summary>
/// Collation choice for indexes and ad-hoc query expressions. SQLite ships three built-in
/// collations: <c>BINARY</c>, <c>NOCASE</c> and <c>RTRIM</c>. Custom collations registered
/// through <c>sqlite3_create_collation</c> are not addressed by this enum.
/// </summary>
public enum SQLiteCollation
{
    /// <summary>
    /// Default. Emits no <c>COLLATE</c> clause. The surrounding context (column declaration,
    /// outer expression) decides which collation applies. Use this when you want the index to
    /// follow the column's declared collation.
    /// </summary>
    Inherit,

    /// <summary>
    /// Forces byte-by-byte comparison. Emits <c>COLLATE BINARY</c>. Use this to override a
    /// column's declared collation.
    /// </summary>
    Binary,

    /// <summary>
    /// ASCII case-insensitive. Emits <c>COLLATE NOCASE</c>.
    /// </summary>
    NoCase,

    /// <summary>
    /// Trailing spaces are ignored. Emits <c>COLLATE RTRIM</c>.
    /// </summary>
    Rtrim,
}
