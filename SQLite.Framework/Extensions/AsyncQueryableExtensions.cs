namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for terminal LINQ operators on <see cref="IQueryable{T}" />.
/// </summary>
public static class AsyncQueryableExtensions
{
    private const string LastUnsupported = "LastAsync is not supported on a SQLite-backed queryable. Use OrderByDescending(...).FirstAsync(...) (or FirstOrDefaultAsync) instead.";
    private const string ContainsComparerUnsupported = "ContainsAsync with a custom IEqualityComparer is not supported on a SQLite-backed queryable. Materialize with ToListAsync() first, then call Contains in memory.";
    private const string SequenceEqualUnsupported = "SequenceEqualAsync is not supported on a SQLite-backed queryable. Materialize with ToListAsync() first, then call SequenceEqual in memory.";
    private const string MinMaxComparerUnsupported = "MinAsync/MaxAsync with a custom IComparer is not supported on a SQLite-backed queryable. Materialize with ToListAsync() first.";
    private const string MinMaxByUnsupported = "MinByAsync/MaxByAsync is not supported because it would override your OrderBy clause. Use OrderBy(...).FirstOrDefaultAsync() (or OrderByDescending) instead.";
    private const string AggregateUnsupported = "AggregateAsync is not supported on a SQLite-backed queryable. Materialize with ToListAsync() first, then call Aggregate in memory.";

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await table.Database.LockAsync(ct);
            return source.ExecuteDelete();
        }, ct);
    }

    /// <summary>
    /// Runs <c>EXPLAIN QUERY PLAN</c> for <paramref name="source" /> and returns the result
    /// as a tree. Requires SQLite 3.24.0 or newer for the four-column row format the helper
    /// parses.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios12.0")]
#endif
    public static Task<SQLiteQueryPlan> ExplainQueryPlanAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await table.Database.ReadLockAsync(ct);
            return source.ExplainQueryPlan();
        }, ct);
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await table.Database.LockAsync(ct);
            return source.ExecuteDelete(predicate);
        }, ct);
    }

    /// <summary>
    /// Executes the query and updates the records in the database.
    /// </summary>
    public static Task<int> ExecuteUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await table.Database.LockAsync(ct);
            return source.ExecuteUpdate(setters);
        }, ct);
    }

    /// <summary>
    /// Runs <c>DELETE ... RETURNING</c> against the filtered source and returns the deleted rows,
    /// each projected through the wrapper's projection. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> ExecuteDeleteAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningQueryable<T, TResult> returning, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.ExecuteDelete();
        }, ct);
    }

    /// <summary>
    /// Applies <paramref name="setters" /> as an <c>UPDATE ... RETURNING</c> against the filtered
    /// source and returns the updated rows, each projected through the wrapper's projection.
    /// Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> ExecuteUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningQueryable<T, TResult> returning, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);
        ArgumentNullException.ThrowIfNull(setters);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.ExecuteUpdate(setters);
        }, ct);
    }

    /// <summary>
    /// Inserts <paramref name="item" /> and returns the inserted row, projected through the
    /// wrapper's projection. Returns <see langword="default" /> when an <c>OnAdd</c> hook cancels the
    /// write, and copies an auto-increment primary key back to <paramref name="item" /> when the
    /// projection materializes <typeparamref name="T" /> in full. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<TResult?> AddAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, T item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.Add(item);
        }, ct);
    }

    /// <summary>
    /// Inserts every item in <paramref name="collection" /> and returns the inserted rows, each
    /// projected through the wrapper's projection. Runs inside a transaction by default. Requires
    /// SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> AddRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.AddRange(collection, runInTransaction);
        }, ct);
    }

    /// <summary>
    /// Updates the row identified by <paramref name="item" />'s primary key and returns the
    /// post-update row, projected through the wrapper's projection. Returns
    /// <see langword="default" /> when no row matched or an <c>OnUpdate</c> hook cancelled the write.
    /// Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<TResult?> UpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, T item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.Update(item);
        }, ct);
    }

    /// <summary>
    /// Updates every item in <paramref name="collection" /> by primary key and returns the
    /// post-update rows, each projected through the wrapper's projection. Runs inside a transaction
    /// by default. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> UpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.UpdateRange(collection, runInTransaction);
        }, ct);
    }

    /// <summary>
    /// Deletes the row identified by <paramref name="item" />'s primary key and returns the deleted
    /// row, projected through the wrapper's projection. Returns <see langword="default" /> when no
    /// row matched or an <c>OnRemove</c> hook cancelled the write. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<TResult?> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, T item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.Remove(item);
        }, ct);
    }

    /// <summary>
    /// Deletes every item in <paramref name="collection" /> by primary key and returns the deleted
    /// rows, each projected through the wrapper's projection. Runs inside a transaction by default.
    /// Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> RemoveRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, IEnumerable<T> collection, bool runInTransaction = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.RemoveRange(collection, runInTransaction);
        }, ct);
    }

    /// <summary>
    /// Runs an <c>INSERT ... ON CONFLICT (...) DO ...</c> upsert built through
    /// <paramref name="configure" /> and returns the written row, projected through the wrapper's
    /// projection. Returns <see langword="default" /> when the conflict resolves to no write (a
    /// <c>DO NOTHING</c>, or a failed <c>DO UPDATE ... WHERE</c> guard) or an <c>OnAddOrUpdate</c>
    /// hook cancels. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<TResult?> UpsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, T item, Action<UpsertBuilder<T>> configure, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.Upsert(item, configure);
        }, ct);
    }

    /// <summary>
    /// Runs the configured upsert for every item in <paramref name="collection" /> and returns the
    /// written rows, each projected through the wrapper's projection. Rows whose conflict resolves to
    /// no write contribute nothing. Runs inside a transaction by default. Requires SQLite 3.35 or later.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<List<TResult>> UpsertRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningTable<T, TResult> returning, IEnumerable<T> collection, Action<UpsertBuilder<T>> configure, bool runInTransaction = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(returning);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await returning.Database.LockAsync(ct);
            return returning.UpsertRange(collection, configure, runInTransaction);
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="Dictionary{TKey, TValue}" />.
    /// </summary>
    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken ct = default)
        where TKey : notnull
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToDictionary(keySelector, elementSelector);
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="Dictionary{TKey, TValue}" />.
    /// </summary>
    public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken ct = default)
        where TKey : notnull
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToDictionary(keySelector);
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to an <see cref="Array" /> of <typeparamref name="T" />.
    /// </summary>
    public static Task<T[]> ToArrayAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToArray();
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="List{T}" />.
    /// </summary>
    public static Task<List<T>> ToListAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToList();
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="HashSet{T}" />.
    /// </summary>
    public static Task<HashSet<T>> ToHashSetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToHashSet();
        }, ct);
    }

    /// <summary>
    /// Converts the <see cref="IEnumerable{T}" /> to a <see cref="ILookup{TKey, TElement}" />.
    /// </summary>
    public static Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken ct = default)
        where TKey : notnull
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ToLookup(keySelector, elementSelector);
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws if the sequence is empty.
    /// </summary>
    public static Task<TSource> FirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.First();
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies the predicate, or throws if no match is found.
    /// </summary>
    public static Task<TSource> FirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.First(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or <see langword="default" /> if the sequence is empty.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.FirstOrDefault();
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or <paramref name="defaultValue" /> if the sequence is empty.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, TSource defaultValue, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.FirstOrDefault(defaultValue);
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies the predicate, or <see langword="default" /> if no match is found.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.FirstOrDefault(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies the predicate, or <paramref name="defaultValue" /> if no match is found.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.FirstOrDefault(predicate, defaultValue);
        }, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws if the sequence is empty or has more than one element.
    /// </summary>
    public static Task<TSource> SingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Single();
        }, ct);
    }

    /// <summary>
    /// Returns the only element matching the predicate, or throws if no match or more than one match is found.
    /// </summary>
    public static Task<TSource> SingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Single(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or <see langword="default" /> if empty. Throws if more than one element.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.SingleOrDefault();
        }, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or <paramref name="defaultValue" /> if empty. Throws if more than one element.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, TSource defaultValue, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.SingleOrDefault(defaultValue);
        }, ct);
    }

    /// <summary>
    /// Returns the only element matching the predicate, or <see langword="default" /> if no match. Throws if more than one match.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.SingleOrDefault(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the only element matching the predicate, or <paramref name="defaultValue" /> if no match. Throws if more than one match.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.SingleOrDefault(predicate, defaultValue);
        }, ct);
    }

    /// <summary>
    /// Returns the element at the given zero-based <paramref name="index" />, or throws if the index is out of range.
    /// </summary>
    public static Task<TSource> ElementAtAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, int index, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ElementAt(index);
        }, ct);
    }

    /// <summary>
    /// Returns the element at the given zero-based <paramref name="index" />, or <see langword="default" /> if out of range.
    /// </summary>
    public static Task<TSource?> ElementAtOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IQueryable<TSource> source, int index, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.ElementAtOrDefault(index);
        }, ct);
    }

    /// <summary>
    /// Returns whether the sequence contains the given <paramref name="item" />.
    /// </summary>
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Contains(item);
        }, ct);
    }

    /// <summary>
    /// Returns whether the sequence contains any elements.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Any();
        }, ct);
    }

    /// <summary>
    /// Returns whether any element of the sequence satisfies the predicate.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Any(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns whether every element of the sequence satisfies the predicate.
    /// </summary>
    public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.All(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the number of elements in the sequence.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Count();
        }, ct);
    }

    /// <summary>
    /// Returns the number of elements in the sequence that satisfy the predicate.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Count(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the number of elements in the sequence as a <see cref="long" />.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.LongCount();
        }, ct);
    }

    /// <summary>
    /// Returns the number of elements in the sequence that satisfy the predicate as a <see cref="long" />.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.LongCount(predicate);
        }, ct);
    }

    /// <summary>
    /// Returns the minimum value in the sequence.
    /// </summary>
    public static Task<TSource?> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Min();
        }, ct);
    }

    /// <summary>
    /// Returns the minimum value of the projected results.
    /// </summary>
    public static Task<TResult?> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Min(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the maximum value in the sequence.
    /// </summary>
    public static Task<TSource?> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Max();
        }, ct);
    }

    /// <summary>
    /// Returns the maximum value of the projected results.
    /// </summary>
    public static Task<TResult?> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Max(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the values in the sequence.
    /// </summary>
    public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the average of the values in the sequence.
    /// </summary>
    public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average();
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Sum(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Average(selector);
        }, ct);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource?> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource?> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(LastUnsupported, error: true)]
    public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue, CancellationToken ct = default)
    {
        throw new NotSupportedException(LastUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(ContainsComparerUnsupported, error: true)]
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(ContainsComparerUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(SequenceEqualUnsupported, error: true)]
    public static Task<bool> SequenceEqualAsync<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, CancellationToken ct = default)
    {
        throw new NotSupportedException(SequenceEqualUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(SequenceEqualUnsupported, error: true)]
    public static Task<bool> SequenceEqualAsync<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(SequenceEqualUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxComparerUnsupported, error: true)]
    public static Task<TSource?> MinAsync<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxComparerUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxComparerUnsupported, error: true)]
    public static Task<TSource?> MaxAsync<TSource>(this IQueryable<TSource> source, IComparer<TSource>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxComparerUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxByUnsupported, error: true)]
    public static Task<TSource?> MinByAsync<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxByUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxByUnsupported, error: true)]
    public static Task<TSource?> MinByAsync<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxByUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxByUnsupported, error: true)]
    public static Task<TSource?> MaxByAsync<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxByUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(MinMaxByUnsupported, error: true)]
    public static Task<TSource?> MaxByAsync<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey>? comparer, CancellationToken ct = default)
    {
        throw new NotSupportedException(MinMaxByUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(AggregateUnsupported, error: true)]
    public static Task<TSource> AggregateAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func, CancellationToken ct = default)
    {
        throw new NotSupportedException(AggregateUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(AggregateUnsupported, error: true)]
    public static Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, CancellationToken ct = default)
    {
        throw new NotSupportedException(AggregateUnsupported);
    }

    /// <summary>Not supported on a SQLite-backed queryable.</summary>
    [Obsolete(AggregateUnsupported, error: true)]
    public static Task<TResult> AggregateAsync<TSource, TAccumulate, TResult>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector, CancellationToken ct = default)
    {
        throw new NotSupportedException(AggregateUnsupported);
    }

    /// <summary>
    /// Async variant of <see cref="QueryableExtensions.StringJoin{T}" />. Emits a single
    /// <c>SELECT group_concat(column, separator) FROM ...</c> SQL query and returns the
    /// concatenated string. Returns an empty string when the source has no rows.
    /// </summary>
    public static Task<string> StringJoinAsync<T>(this IQueryable<T> source, string separator, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.StringJoin(separator);
        }, ct);
    }

    /// <summary>
    /// Async variant of <see cref="QueryableExtensions.Total{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})" />.
    /// Emits a single <c>SELECT total(column) FROM ...</c> SQL query and returns the sum as a
    /// <see cref="double" />. Returns <c>0.0</c> when the source has no rows.
    /// </summary>
    public static Task<double> TotalAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Total(selector);
        }, ct);
    }

    /// <summary>
    /// Async variant of <see cref="QueryableExtensions.Total{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})" />.
    /// </summary>
    public static Task<double> TotalAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Total(selector);
        }, ct);
    }

    /// <summary>
    /// Async variant of <see cref="QueryableExtensions.Total{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})" />.
    /// </summary>
    public static Task<double> TotalAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Total(selector);
        }, ct);
    }

    /// <summary>
    /// Async variant of <see cref="QueryableExtensions.Total{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})" />.
    /// </summary>
    public static Task<double> TotalAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await sqliteSource.Database.ReadLockAsync(ct);
            return source.Total(selector);
        }, ct);
    }
}
