namespace SQLite.Framework.Attributes;

/// <summary>
/// Indicates that a table does not have a RowId within the table.
/// </summary>
/// <remarks>
/// For more information, see the SQLite documentation https://sqlite.org/withoutrowid.html
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class WithoutRowIdAttribute : Attribute;