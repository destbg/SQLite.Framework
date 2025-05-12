namespace SQLite.Framework.Internals.Models;

/// <summary>
/// The compiled SQL query.
/// </summary>
internal class SQLQuery
{
    public required string Sql { get; init; }
    public required List<SQLiteParameter> Parameters { get; init; }
    public required Func<QueryContext, dynamic?>? CreateObject { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
}