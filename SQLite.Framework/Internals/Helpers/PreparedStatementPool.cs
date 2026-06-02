namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// A bounded pool of prepared <c>sqlite3_stmt</c> handles keyed by SQL text. Repeated queries reuse
/// the compiled statement instead of preparing and finalizing a fresh one each time, which skips the
/// SQL parse and bytecode build inside SQLite on every call.
/// </summary>
/// <remarks>
/// A rented statement is removed from the pool and owned by a single caller until it is returned, so
/// two concurrent readers of the same SQL always get distinct statements. SQLite statements are not
/// safe to step from two threads at once, so this ownership rule is what keeps concurrent reads
/// correct. The pool holds at most one statement per SQL string and at most
/// <see cref="MaxStatements" /> in total. When it is full, the least recently used statement is
/// finalized. This stops one-shot SQL such as unique <c>SAVEPOINT</c> names from pushing the hot
/// query statements out of the pool.
/// </remarks>
internal sealed class PreparedStatementPool
{
    private const int MaxStatements = 128;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock, it doesn't exist in .NET 8
    private readonly object gate = new();
    private readonly Dictionary<string, sqlite3_stmt> free = new(StringComparer.Ordinal);
    private readonly LinkedList<string> lru = new();
    private readonly Dictionary<string, LinkedListNode<string>> lruNodes = new(StringComparer.Ordinal);
    private bool disposed;

    /// <summary>
    /// Tries to take the pooled statement for <paramref name="sql" />. Returns false when the pool has
    /// none, in which case the caller prepares a new statement.
    /// </summary>
    public bool TryRent(string sql, out sqlite3_stmt statement)
    {
        lock (gate)
        {
            if (free.TryGetValue(sql, out statement!))
            {
                free.Remove(sql);
                lru.Remove(lruNodes[sql]);
                lruNodes.Remove(sql);
                return true;
            }
        }

        statement = default!;
        return false;
    }

    /// <summary>
    /// Resets <paramref name="statement" /> and returns it to the pool. It is finalized instead of
    /// pooled when the pool has been cleared or already holds a statement for this SQL. When the pool
    /// would exceed <see cref="MaxStatements" />, the least recently used statement is finalized.
    /// </summary>
    public void Return(string sql, sqlite3_stmt statement)
    {
        raw.sqlite3_reset(statement);
        raw.sqlite3_clear_bindings(statement);

        sqlite3_stmt? toFinalize;
        lock (gate)
        {
            if (disposed || free.ContainsKey(sql))
            {
                toFinalize = statement;
            }
            else
            {
                free[sql] = statement;
                lruNodes[sql] = lru.AddFirst(sql);
                toFinalize = free.Count > MaxStatements ? EvictLeastRecentlyUsed() : null;
            }
        }

        if (toFinalize != null)
        {
            raw.sqlite3_finalize(toFinalize);
        }
    }

    /// <summary>
    /// Finalizes every pooled statement and marks the pool closed. Call this before the connection is
    /// closed so SQLite has no pooled statements left.
    /// </summary>
    public void Clear()
    {
        List<sqlite3_stmt> toFinalize = [];
        lock (gate)
        {
            disposed = true;
            toFinalize.AddRange(free.Values);
            free.Clear();
            lru.Clear();
            lruNodes.Clear();
        }

        foreach (sqlite3_stmt statement in toFinalize)
        {
            raw.sqlite3_finalize(statement);
        }
    }

    // Removes the least recently used statement. The caller finalizes it outside the lock. Only called
    // while the pool is over its size limit, so the LRU list is never empty here.
    private sqlite3_stmt EvictLeastRecentlyUsed()
    {
        string key = lru.Last!.Value;
        sqlite3_stmt evicted = free[key];
        free.Remove(key);
        lru.RemoveLast();
        lruNodes.Remove(key);
        return evicted;
    }
}
