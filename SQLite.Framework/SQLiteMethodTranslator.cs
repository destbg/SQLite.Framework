namespace SQLite.Framework;

/// <summary>
/// Translates a custom method call into a SQL fragment.
/// </summary>
/// <param name="instanceSql">The SQL for the instance the method is called on, or <c>null</c> for static methods.</param>
/// <param name="argumentsSql">The SQL fragments for each argument.</param>
/// <returns>A SQL string representing the method call.</returns>
public delegate string SQLiteMethodTranslator(string? instanceSql, string[] argumentsSql);