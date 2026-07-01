namespace SQLite.Framework.Attributes;

/// <summary>
/// Indicates that a property should be indexed in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IndexedAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedAttribute"/> class with the specified name, order and uniqueness.
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

    /// <summary>
    /// Collation applied to this column inside the index. The default
    /// <see cref="SQLiteCollation.Inherit" /> emits no clause so the column's declared collation
    /// wins. Set to <see cref="SQLiteCollation.NoCase" /> or <see cref="SQLiteCollation.Rtrim" />
    /// to make the index reusable for case-insensitive or trailing-space-insensitive lookups.
    /// Set to <see cref="SQLiteCollation.Binary" /> to force binary comparison even when the
    /// column declares a different collation.
    /// </summary>
    public SQLiteCollation Collation { get; init; }

    /// <summary>
    /// Sort direction for this column inside the index. The default
    /// <see cref="SQLiteIndexDirection.Inherit" /> emits no clause. Set to
    /// <see cref="SQLiteIndexDirection.Descending" /> to store the column in reverse order so
    /// that the query planner can read the index forward for matching <c>ORDER BY x DESC</c>
    /// clauses without a separate sort step.
    /// </summary>
    public SQLiteIndexDirection Direction { get; init; }
}