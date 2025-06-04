namespace SQLite.Framework.Attributes;

/// <summary>
/// Indicates that a property should be indexed in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IndexedAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedAttribute"/> class with the specified name, order, and uniqueness.
    /// </summary>
    public IndexedAttribute(string name, int order)
    {
        Name = name;
        Order = order;
        IsUnique = false; // Default value for Unique
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedAttribute"/> class without parameters.
    /// </summary>
    public IndexedAttribute()
    {
    }

    /// <summary>
    /// The name of the index. If not specified, the property name will be used.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The order of the indexed property in the index. This is used when multiple properties are indexed together.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// The order of the indexed property in the index. This is used when multiple properties are indexed together.
    /// </summary>
    public int Order { get; init; }
}