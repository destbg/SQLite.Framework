namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a property on an <see cref="RTreeIndexAttribute" /> class as an auxiliary column. The
/// framework emits the column with the SQLite <c>+</c> prefix. Auxiliary columns are stored next
/// to the bounding box but do not participate in the spatial index, so they cannot be used in
/// the query planner's R-Tree lookups. Requires SQLite 3.24.0 or newer.
/// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android29.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios12.0")]
#endif
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RTreeAuxiliaryAttribute : Attribute;
