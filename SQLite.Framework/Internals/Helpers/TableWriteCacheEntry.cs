namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// One cached single-item write shape for <see cref="SQLiteTable{T}" />. Holds the columns to
/// bind, the finished SQL and the bind delegate so a repeated <c>Add</c>, <c>Update</c>,
/// <c>Remove</c> or <c>AddOrUpdate</c> call skips the per-call SQL build and parameter list.
/// </summary>
internal sealed class TableWriteCacheEntry<T>
{
    public required TableColumn[] Columns { get; init; }
    public required string Sql { get; init; }
    public required Action<sqlite3_stmt, T> BindRow { get; init; }
    public TableColumn? AutoIncrement { get; init; }
}
