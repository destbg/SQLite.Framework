namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a property on an FTS5 entity class as an indexed column.
/// Properties without this attribute are ignored when building the virtual table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FullTextIndexedAttribute : Attribute
{
    /// <summary>
    /// The BM25 weight for this column. Higher weights make matches in this column score higher.
    /// Defaults to <c>1.0</c>.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// When <see langword="true" />, the column is stored but not indexed. Useful for projecting
    /// extra metadata back into your results without making it searchable.
    /// </summary>
    public bool Unindexed { get; set; }
}
