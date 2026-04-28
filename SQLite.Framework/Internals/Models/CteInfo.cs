namespace SQLite.Framework.Internals.Models;

[ExcludeFromCodeCoverage]
internal class CteInfo
{
    public required string Name { get; init; }
    public required string Sql { get; init; }
    public required SQLiteParameter[]? Parameters { get; init; }
    public bool IsRecursive { get; init; }
}
