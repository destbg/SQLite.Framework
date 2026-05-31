namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for <see cref="SQLiteTable{T}" />.
/// </summary>
public static class AsyncSQLiteTableExtensions
{
    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public static Task<int> AddAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.Add(item);
        }, ct);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        return RunRangeAsync(source, collection, runInTransaction, source.AddRange, ct);
    }

    /// <summary>
    /// Copies rows from <paramref name="query" /> into <paramref name="source" /> using a single
    /// <c>INSERT INTO ... SELECT</c> statement.
    /// </summary>
    public static Task<int> InsertFromQueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IQueryable<T> query, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.InsertFromQuery(query);
        }, ct);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public static Task<int> UpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.Update(item);
        }, ct);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public static Task<int> UpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        return RunRangeAsync(source, collection, runInTransaction, source.UpdateRange, ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public static Task<int> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.Remove(item);
        }, ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public static Task<int> RemoveRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        return RunRangeAsync(source, collection, runInTransaction, source.RemoveRange, ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR REPLACE</c> operation on the database table using the row.
    /// </summary>
    public static Task<int> AddOrUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AddOrUpdateAsync(source, item, SQLiteConflict.Replace, ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the row.
    /// </summary>
    public static Task<int> AddOrUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, SQLiteConflict conflict, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.AddOrUpdate(item, conflict);
        }, ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR REPLACE</c> operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddOrUpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        return AddOrUpdateRangeAsync(source, collection, SQLiteConflict.Replace, runInTransaction, ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddOrUpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, SQLiteConflict conflict, bool runInTransaction = true, CancellationToken ct = default)
    {
        return RunRangeAsync(source, collection, runInTransaction,
            (c, t) => source.AddOrUpdateRange(c, t, conflict), ct);
    }

    /// <summary>
    /// Performs an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> upsert built through the
    /// <see cref="SQLiteUpsertBuilder{T}" /> DSL.
    /// </summary>
    public static Task<int> UpsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, Action<SQLiteUpsertBuilder<T>> configure, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.Upsert(item, configure);
        }, ct);
    }

    /// <summary>
    /// Range version of <see cref="UpsertAsync{T}(SQLiteTable{T}, T, Action{SQLiteUpsertBuilder{T}}, CancellationToken)" />.
    /// </summary>
    public static Task<int> UpsertRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, Action<SQLiteUpsertBuilder<T>> configure, bool runInTransaction = true, CancellationToken ct = default)
    {
        return RunRangeAsync(source, collection, runInTransaction,
            (c, t) => source.UpsertRange(c, configure, t), ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public static Task<int> ClearAsync(this SQLiteTable source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await source.Database.LockAsync(ct);
            return source.Clear();
        }, ct);
    }

    private static Task<int> RunRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction, Func<IEnumerable<T>, bool, int> sync, CancellationToken ct)
    {
        return AsyncRunner.Run(async () =>
        {
            if (!runInTransaction)
            {
                using IDisposable _ = await source.Database.LockAsync(ct);
                return sync(collection, false);
            }

            SQLiteTransaction transaction = await source.Database.BeginTransactionAsync(ct);
            try
            {
                int count = sync(collection, false);
                await transaction.CommitAsync(ct);
                return count;
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }, ct);
    }
}
