using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// The compiled SQL query.
/// </summary>
[ExcludeFromCodeCoverage]
internal class SQLQuery
{
    public required string Sql { get; init; }
    public required List<SQLiteParameter> Parameters { get; init; }
    public required Func<QueryContext, dynamic?>? CreateObject { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
}