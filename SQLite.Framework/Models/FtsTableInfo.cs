using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Models;

/// <summary>
/// FTS5-specific metadata read from a class decorated with <see cref="FullTextSearchAttribute" />.
/// Set on <see cref="TableMapping.FullTextSearch" /> when the table is an FTS5 virtual table.
/// </summary>
public sealed class FtsTableInfo
{
    /// <summary>
    /// Initializes the FTS metadata for an entity class.
    /// </summary>
    public FtsTableInfo(FullTextSearchAttribute attribute, IReadOnlyList<FtsIndexedColumn> indexedColumns, FtsRowIdInfo? rowId, string tokenizerClause)
    {
        Attribute = attribute;
        IndexedColumns = indexedColumns;
        RowId = rowId;
        TokenizerClause = tokenizerClause;
    }

    /// <summary>
    /// The original <see cref="FullTextSearchAttribute" /> read from the class.
    /// </summary>
    public FullTextSearchAttribute Attribute { get; }

    /// <summary>
    /// The columns that participate in the virtual table, in declaration order.
    /// </summary>
    public IReadOnlyList<FtsIndexedColumn> IndexedColumns { get; }

    /// <summary>
    /// The property mapped to the implicit <c>rowid</c> column, if any.
    /// </summary>
    public FtsRowIdInfo? RowId { get; }

    /// <summary>
    /// The pre-rendered <c>tokenize='...'</c> clause for the <c>CREATE VIRTUAL TABLE</c> statement.
    /// </summary>
    public string TokenizerClause { get; }

    /// <summary>
    /// Convenience: where the indexed values come from.
    /// </summary>
    public FtsContentMode ContentMode => Attribute.ContentMode;

    /// <summary>
    /// Convenience: how the FTS table is kept in sync with the source table.
    /// </summary>
    public FtsAutoSync AutoSync => Attribute.AutoSync;
}
