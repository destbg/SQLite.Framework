namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a method as a wrapper around a SQLite pragma table-valued function such as
/// <c>pragma_table_info(name)</c>. When the method is used inside a LINQ expression tree
/// (for example, as the inner source of a <c>SelectMany</c> or a <c>join</c>), the
/// translator emits the pragma call directly so the argument can reference columns from
/// the outer query. When the method is invoked at runtime, the wrapper body is used
/// (typically via <c>FromSql</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SQLitePragmaFunctionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLitePragmaFunctionAttribute"/> class.
    /// </summary>
    /// <param name="sqlName">The pragma function name, for example <c>pragma_table_info</c>.</param>
    public SQLitePragmaFunctionAttribute(string sqlName)
    {
        SqlName = sqlName;
    }

    /// <summary>
    /// The pragma function name to emit in SQL.
    /// </summary>
    public string SqlName { get; }
}
