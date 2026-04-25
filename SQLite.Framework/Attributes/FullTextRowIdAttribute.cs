namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks an <c>int</c> or <c>long</c> property on an FTS5 entity class as the alias for the
/// implicit <c>rowid</c> column. When omitted, the FTS5 row's <c>rowid</c> is not exposed
/// as a property on the entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FullTextRowIdAttribute : Attribute;
