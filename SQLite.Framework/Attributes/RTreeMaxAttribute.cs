namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a property on an <see cref="RTreeIndexAttribute" /> class as the upper bound of an
/// R-Tree dimension. Must be paired with a <see cref="RTreeMinAttribute" /> that shares the same
/// <see cref="Dimension" /> name. SQLite allows 1 to 5 dimensions.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RTreeMaxAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RTreeMaxAttribute" /> class for the given
    /// dimension. Dimensions are matched by name across the min and max attributes.
    /// </summary>
    public RTreeMaxAttribute(string dimension)
    {
        Dimension = dimension;
    }

    /// <summary>
    /// Name of the R-Tree dimension this property is the upper bound for. Common values are
    /// <c>"X"</c>, <c>"Y"</c>, <c>"Z"</c>, but any string that matches the paired
    /// <see cref="RTreeMinAttribute" /> works.
    /// </summary>
    public string Dimension { get; }
}
