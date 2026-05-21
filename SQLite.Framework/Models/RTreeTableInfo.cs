namespace SQLite.Framework.Models;

/// <summary>
/// R-Tree metadata read from a class decorated with <see cref="RTreeIndexAttribute" />.
/// Set on <see cref="TableMapping.RTree" /> when the table is an R-Tree virtual table.
/// </summary>
public sealed class RTreeTableInfo
{
    /// <summary>
    /// Initializes the R-Tree metadata for an entity class.
    /// </summary>
    public RTreeTableInfo(RTreeIndexAttribute attribute, PropertyInfo rowIdProperty, string rowIdColumnName, IReadOnlyList<RTreeBoundsColumn> bounds, IReadOnlyList<RTreeAuxiliaryColumn> auxiliaries)
    {
        Attribute = attribute;
        RowIdProperty = rowIdProperty;
        RowIdColumnName = rowIdColumnName;
        Bounds = bounds;
        Auxiliaries = auxiliaries;
    }

    /// <summary>
    /// The original <see cref="RTreeIndexAttribute" /> read from the class.
    /// </summary>
    public RTreeIndexAttribute Attribute { get; }

    /// <summary>
    /// The CLR property mapped to the R-Tree rowid (the primary key of the virtual table).
    /// </summary>
    public PropertyInfo RowIdProperty { get; }

    /// <summary>
    /// SQL column name for the rowid.
    /// </summary>
    public string RowIdColumnName { get; }

    /// <summary>
    /// Bounding-box slots in emission order (min, max, min, max, ...).
    /// </summary>
    public IReadOnlyList<RTreeBoundsColumn> Bounds { get; }

    /// <summary>
    /// Auxiliary columns in declaration order. Empty when none are declared.
    /// </summary>
    public IReadOnlyList<RTreeAuxiliaryColumn> Auxiliaries { get; }

    /// <summary>
    /// Storage variant. <see cref="SQLiteRTreeStorage.Float" /> emits
    /// <c>USING rtree(...)</c>, <see cref="SQLiteRTreeStorage.Int32" /> emits
    /// <c>USING rtree_i32(...)</c>.
    /// </summary>
    public SQLiteRTreeStorage Storage => Attribute.Storage;
}
