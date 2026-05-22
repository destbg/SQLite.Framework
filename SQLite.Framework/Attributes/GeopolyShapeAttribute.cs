namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks the property on a <see cref="GeopolyIndexAttribute" /> entity that maps to the
/// implicit <c>_shape</c> column. Exactly one property per entity must carry this attribute.
/// Use a <see cref="string" /> property for GeoJSON text, or <see cref="byte" />[] for the
/// binary polygon format produced by <c>geopoly_blob()</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GeopolyShapeAttribute : Attribute;
