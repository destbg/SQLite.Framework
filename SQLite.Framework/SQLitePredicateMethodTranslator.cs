namespace SQLite.Framework;

/// <summary>
/// Translates a .NET method call that includes a predicate lambda into a SQL fragment.
/// </summary>
/// <param name="instanceSql">The SQL for the collection instance, or null for static methods.</param>
/// <param name="predicateSql">The translated SQL of the lambda body, with the lambda parameter bound to <c>value</c> from <c>json_each</c>.</param>
/// <returns>A SQL fragment representing the method call.</returns>
public delegate string SQLitePredicateMethodTranslator(string? instanceSql, string predicateSql);
