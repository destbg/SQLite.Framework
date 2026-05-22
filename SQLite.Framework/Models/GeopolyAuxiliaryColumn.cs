namespace SQLite.Framework.Models;

/// <summary>
/// An auxiliary column on a Geopoly virtual table. Auxiliary columns are stored next to the
/// polygon and the rowid but do not participate in the spatial index.
/// </summary>
public sealed class GeopolyAuxiliaryColumn
{
    /// <summary>
    /// Initializes a new <see cref="GeopolyAuxiliaryColumn" />.
    /// </summary>
    public GeopolyAuxiliaryColumn(PropertyInfo property, string columnName)
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
