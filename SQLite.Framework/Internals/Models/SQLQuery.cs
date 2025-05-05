namespace SQLite.Framework.Internals.Models;

internal class SQLQuery
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
}