using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Passed to the <see cref="CompiledExpression"/> functions to provide
/// support for select methods using both SQL and LINQ expressions.
/// </summary>
[ExcludeFromCodeCoverage]
internal class QueryContext
{
    public SQLiteDataReader? Reader { get; init; }
    public Dictionary<string, int>? Columns { get; init; }
    public object? Input { get; init; }
}