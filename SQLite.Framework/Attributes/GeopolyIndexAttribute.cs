namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a class as a Geopoly virtual table. Geopoly is SQLite's geospatial polygon module,
/// built on top of R-Tree. Each row stores a polygon and any number of auxiliary columns, and
/// queries use the <c>geopoly_*</c> SQL functions (see <see cref="SQLiteGeopolyFunctions" />).
/// Requires SQLite 3.27.0 or newer and a build that has the Geopoly extension compiled in.
/// </summary>
/// <remarks>
/// The mapped class must have:
/// <list type="bullet">
/// <item>Exactly one <c>[Key]</c> property of type <see cref="int" /> or <see cref="long" />,
/// which maps to the implicit Geopoly rowid.</item>
/// <item>Exactly one property marked with <see cref="GeopolyShapeAttribute" />, which maps to
/// the implicit <c>_shape</c> column. The property can be a <see cref="string" /> (GeoJSON
/// text) or a <see cref="byte" />[] (the binary polygon format SQLite emits via
/// <c>geopoly_blob()</c>).</item>
/// <item>Zero or more additional properties, which become Geopoly auxiliary columns. Mark a
/// property with <c>[NotMapped]</c> to keep it out of the schema.</item>
/// </list>
/// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android30.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios13.0")]
#endif
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GeopolyIndexAttribute : Attribute;
