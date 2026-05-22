namespace SQLite.Framework.Models;

/// <summary>
/// Geopoly metadata read from a class decorated with <see cref="GeopolyIndexAttribute" />.
/// Set on <see cref="TableMapping.Geopoly" /> when the table is a Geopoly virtual table.
/// </summary>
public sealed class GeopolyTableInfo
{
    /// <summary>
    /// Initializes the Geopoly metadata for an entity class.
    /// </summary>
    public GeopolyTableInfo(PropertyInfo rowIdProperty, PropertyInfo shapeProperty, IReadOnlyList<GeopolyAuxiliaryColumn> auxiliaries)
    {
        RowIdProperty = rowIdProperty;
        ShapeProperty = shapeProperty;
        Auxiliaries = auxiliaries;
    }

    /// <summary>
    /// The CLR property mapped to the implicit Geopoly rowid (the entity's <c>[Key]</c>).
    /// </summary>
    public PropertyInfo RowIdProperty { get; }

    /// <summary>
    /// The CLR property mapped to the implicit <c>_shape</c> column.
    /// </summary>
    public PropertyInfo ShapeProperty { get; }

    /// <summary>
    /// Auxiliary columns in declaration order. Empty when none are declared.
    /// </summary>
    public IReadOnlyList<GeopolyAuxiliaryColumn> Auxiliaries { get; }
}
