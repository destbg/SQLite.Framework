namespace SQLite.Framework.Models;

/// <summary>
/// The property on an FTS5 entity that maps to the implicit <c>rowid</c> column.
/// </summary>
public sealed class FtsRowIdInfo
{
    /// <summary>
    /// Initializes a new <see cref="FtsRowIdInfo" />.
    /// </summary>
    public FtsRowIdInfo(PropertyInfo property)
    {
        Property = property;
    }

    /// <summary>
    /// The .NET property the rowid maps to.
    /// </summary>
    public PropertyInfo Property { get; }
}
