namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Non-generic base for <see cref="TableWriteCache{T}" /> so <see cref="TableMapping" />
/// can hold the cache without knowing the entity type.
/// </summary>
internal abstract class TableWriteCache
{
    protected TableWriteCache(SQLiteOptions options)
    {
        Options = options;
    }

    public SQLiteOptions Options { get; }
}

/// <summary>
/// Caches the single-item write shapes for one <see cref="TableMapping" />
/// and one <see cref="SQLiteOptions" /> instance.
/// </summary>
internal sealed class TableWriteCache<T> : TableWriteCache
{
    public TableWriteCache(SQLiteOptions options)
        : base(options)
    {
        AddOrUpdate = new TableWriteCacheEntry<T>?[5];
    }

    public TableWriteCacheEntry<T>? Add { get; set; }
    public TableWriteCacheEntry<T>? Update { get; set; }
    public TableWriteCacheEntry<T>? Remove { get; set; }
    public TableWriteCacheEntry<T>?[] AddOrUpdate { get; }
}
