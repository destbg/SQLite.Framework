namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a class as a SQLite <c>STRICT</c> table. SQLite enforces declared column types on
/// every insert and update instead of accepting any type. Requires SQLite 3.37.0 or newer.
/// </summary>
/// <remarks>
/// For more information, see the SQLite documentation https://sqlite.org/stricttables.html
/// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android34.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios16.0")]
#endif
[AttributeUsage(AttributeTargets.Class)]
public class StrictTableAttribute : Attribute;
