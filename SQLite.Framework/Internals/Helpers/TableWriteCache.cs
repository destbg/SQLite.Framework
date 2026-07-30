namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Non-generic base for <see cref="TableWriteCache{T}" /> so <see cref="TableMapping" />
/// can hold the cache without knowing the entity type.
/// </summary>
internal abstract class TableWriteCache
{
    protected TableWriteCache(SQLiteOptions options, long attachGeneration)
    {
        Options = options;
        AttachGeneration = attachGeneration;
    }

    public SQLiteOptions Options { get; }

    public long AttachGeneration { get; }
}

/// <summary>
/// Caches the single-item write shapes for one <see cref="TableMapping" />
/// and one <see cref="SQLiteOptions" /> instance.
/// </summary>
internal sealed class TableWriteCache<T> : TableWriteCache
{
    public TableWriteCache(SQLiteOptions options, long attachGeneration)
        : base(options, attachGeneration)
    {
        AddOrUpdate = new TableWriteCacheEntry<T>?[5];
    }

    public TableWriteCacheEntry<T>? Add { get; set; }
    public TableWriteCacheEntry<T>? Update { get; set; }
    public TableWriteCacheEntry<T>? Remove { get; set; }
    public TableWriteCacheEntry<T>?[] AddOrUpdate { get; }
}
