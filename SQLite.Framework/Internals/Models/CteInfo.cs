namespace SQLite.Framework.Internals.Models;

internal class CteInfo
{
    public required string Name { get; init; }
    public required string Sql { get; init; }
    public required SQLiteParameter[]? Parameters { get; init; }
    public bool IsRecursive { get; init; }
    public SQLiteCteMaterialization Materialization { get; init; }
    public string[]? ColumnNames { get; init; }
    public string[]? EmittedColumns { get; init; }
    public HashSet<string>? DayOfWeekColumns { get; init; }
    public HashSet<string>? JsonSourceColumns { get; init; }
    public HashSet<string>? ConstructedPaths { get; init; }
    public bool OptionalRow { get; init; }
    public HashSet<string>? OptionalRowPaths { get; init; }
    public Dictionary<string, Expression>? BodyColumns { get; init; }
    public IReadOnlyList<SQLiteExpression>? BodySelects { get; init; }
}
