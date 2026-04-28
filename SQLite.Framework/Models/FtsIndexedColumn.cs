namespace SQLite.Framework.Models;

/// <summary>
/// One indexed column on an FTS5 virtual table.
/// </summary>
public sealed class FtsIndexedColumn
{
    /// <summary>
    /// Initializes a new <see cref="FtsIndexedColumn" />.
    /// </summary>
    public FtsIndexedColumn(PropertyInfo property, string name, double weight, bool unindexed)
    {
        Property = property;
        Name = name;
        Weight = weight;
        Unindexed = unindexed;
    }

    /// <summary>
    /// The .NET property the column maps to.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// The column name as declared in the virtual table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The BM25 weight applied to matches in this column.
    /// </summary>
    public double Weight { get; }

    /// <summary>
    /// When <see langword="true" />, the column is stored but not indexed.
    /// </summary>
    public bool Unindexed { get; }
}
