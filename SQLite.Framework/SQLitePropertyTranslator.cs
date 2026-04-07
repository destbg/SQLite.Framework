namespace SQLite.Framework;

/// <summary>
/// Translates a property access on a custom type into a SQL fragment.
/// </summary>
/// <param name="memberName">The name of the property being accessed.</param>
/// <param name="instanceSql">The SQL expression of the instance the property is accessed on.</param>
/// <returns>A SQL fragment representing the property access, or <c>null</c> if this translator does not handle it.</returns>
public delegate string? SQLitePropertyTranslator(string memberName, string instanceSql);
