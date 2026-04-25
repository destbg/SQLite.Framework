using SQLite.Framework.Enums;

namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a class as an FTS5 virtual table. The class becomes searchable through
/// <c>db.Table&lt;T&gt;()</c> and supports <c>SQLiteFunctions.Match</c>, <c>Rank</c>,
/// <c>Snippet</c>, and <c>Highlight</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FullTextSearchAttribute : Attribute
{
    /// <summary>
    /// Where the indexed column values come from. Defaults to <see cref="FtsContentMode.Internal" />.
    /// </summary>
    public FtsContentMode ContentMode { get; set; } = FtsContentMode.Internal;

    /// <summary>
    /// The source table that owns the column values. Required when
    /// <see cref="ContentMode" /> is <see cref="FtsContentMode.External" />.
    /// </summary>
    public Type? ContentTable { get; set; }

    /// <summary>
    /// Optional override for the <c>content_rowid</c> column on the source table.
    /// When <see langword="null" />, the <c>[Key]</c> property of <see cref="ContentTable" /> is used.
    /// </summary>
    public string? ContentRowIdColumn { get; set; }

    /// <summary>
    /// Whether the framework should keep the FTS table in sync with the source table automatically.
    /// Defaults to <see cref="FtsAutoSync.Manual" />.
    /// </summary>
    public FtsAutoSync AutoSync { get; set; } = FtsAutoSync.Manual;

    /// <summary>
    /// Optional FTS5 <c>prefix</c> table option, for example <c>"2 3"</c> to enable 2-letter and 3-letter
    /// prefix indexes. When <see langword="null" />, no prefix indexes are created.
    /// </summary>
    public string? Prefix { get; set; }
}
