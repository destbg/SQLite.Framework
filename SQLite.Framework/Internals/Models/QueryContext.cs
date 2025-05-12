using SQLite.Framework.Enums;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Passed to the <see cref="CompiledExpression"/> functions to provide
/// support for select methods using both SQL and LINQ expressions.
/// </summary>
internal class QueryContext
{
    public required SQLiteDataReader Reader { get; init; }
    public required Dictionary<string, (int Index, SQLiteColumnType ColumnType)> Columns { get; init; }
}