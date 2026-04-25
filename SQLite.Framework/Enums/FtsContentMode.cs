namespace SQLite.Framework.Enums;

/// <summary>
/// Controls where the values that an FTS5 virtual table indexes come from.
/// </summary>
public enum FtsContentMode
{
    /// <summary>
    /// The FTS table stores its own copy of the column values.
    /// You insert into the FTS table directly. This is the FTS5 default.
    /// </summary>
    Internal,

    /// <summary>
    /// The FTS table reads its column values from another (regular) table.
    /// Set <c>ContentTable</c> on <c>[FullTextSearch]</c> to point at it.
    /// </summary>
    External,

    /// <summary>
    /// The FTS table only stores the search index, not the column values.
    /// You can search but you cannot read the column values back.
    /// </summary>
    Contentless,
}
