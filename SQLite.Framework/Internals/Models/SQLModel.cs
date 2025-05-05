namespace SQLite.Framework.Internals.Models;

internal class SQLModel
{
    public required List<JoinInfo> Joins { get; init; }
    public required List<string> Wheres { get; init; }
    public required List<string> OrderBys { get; init; }
    public required List<string> Selects { get; init; }
    public required List<(string Sql, bool All)> Unions { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
    public required string? From { get; init; }
    public required int? Take { get; init; }
    public required int? Skip { get; init; }
    public required bool IsAny { get; init; }
    public required bool IsAll { get; init; }
    public required bool IsDistinct { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
}