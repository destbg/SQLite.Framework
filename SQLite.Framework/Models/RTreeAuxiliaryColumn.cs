namespace SQLite.Framework.Models;

/// <summary>
/// An auxiliary (non-spatial) column attached to an R-Tree virtual table. SQLite stores these
/// values next to the bounding box but they do not participate in the spatial index. The
/// framework emits them with the SQLite <c>+</c> prefix.
/// </summary>
public sealed class RTreeAuxiliaryColumn
{
    /// <summary>
    /// Initializes a new <see cref="RTreeAuxiliaryColumn" />.
    /// </summary>
    public RTreeAuxiliaryColumn(PropertyInfo property, string columnName)
    {
        Property = property;
        ColumnName = columnName;
    }

    /// <summary>
    /// The CLR property that backs this column.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// SQL column name. Honors <c>[Column("...")]</c>.
    /// </summary>
    public string ColumnName { get; }
}
