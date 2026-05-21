namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a property on an <see cref="RTreeIndexAttribute" /> class as the lower bound of an
/// R-Tree dimension. Must be paired with a <see cref="RTreeMaxAttribute" /> that shares the same
/// <see cref="Dimension" /> name. SQLite allows 1 to 5 dimensions.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RTreeMinAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RTreeMinAttribute" /> class for the given
    /// dimension. Dimensions are matched by name across the min and max attributes.
    /// </summary>
    public RTreeMinAttribute(string dimension)
    {
        Dimension = dimension;
    }

    /// <summary>
    /// Name of the R-Tree dimension this property is the lower bound for. Common values are
    /// <c>"X"</c>, <c>"Y"</c>, <c>"Z"</c>, but any string that matches the paired
    /// <see cref="RTreeMaxAttribute" /> works.
    /// </summary>
    public string Dimension { get; }
}
