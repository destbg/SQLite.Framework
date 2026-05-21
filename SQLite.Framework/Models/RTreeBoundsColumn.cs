namespace SQLite.Framework.Models;

/// <summary>
/// One bounding-box slot of an R-Tree virtual table. Each dimension contributes two slots
/// (min and max). The framework uses this descriptor to emit the column inside
/// <c>CREATE VIRTUAL TABLE ... USING rtree(...)</c> and to bind values on insert and update.
/// </summary>
public sealed class RTreeBoundsColumn
{
    /// <summary>
    /// Initializes a new <see cref="RTreeBoundsColumn" />.
    /// </summary>
    public RTreeBoundsColumn(PropertyInfo property, string columnName, string dimension, bool isMin)
    {
        Property = property;
        ColumnName = columnName;
        Dimension = dimension;
        IsMin = isMin;
    }

    /// <summary>
    /// The CLR property that backs this slot.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// SQL column name. Honors <c>[Column("...")]</c>.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Name of the R-Tree dimension this slot belongs to. Matches the name given to the
    /// <see cref="Attributes.RTreeMinAttribute" /> and <see cref="Attributes.RTreeMaxAttribute" />.
    /// </summary>
    public string Dimension { get; }

    /// <summary>
    /// <see langword="true" /> for the lower bound of the dimension, <see langword="false" /> for
    /// the upper bound.
    /// </summary>
    public bool IsMin { get; }
}
