namespace SQLite.Framework.Internals.Models;

internal class SQLQuery
{
    public required string Sql { get; init; }
    public required List<SQLiteParameter> Parameters { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
}