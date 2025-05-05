namespace SQLite.Framework.Attributes;

/// <summary>
/// Indicates that the property is an auto-incrementing primary key.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AutoIncrementAttribute : Attribute;