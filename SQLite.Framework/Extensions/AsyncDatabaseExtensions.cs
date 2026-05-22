namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for raw-SQL helpers on <see cref="SQLiteDatabase" />.
/// </summary>
public static class AsyncDatabaseExtensions
{
    /// <summary>
    /// Async version of <see cref="SQLiteDatabase.Lock" />.
    /// Waits for the connection lock without blocking a thread.
    /// Dispose the returned <see cref="IDisposable" /> to release the lock.
    /// </summary>
    public static SQLiteLockAwaitable LockAsync(this SQLiteDatabase database, CancellationToken cancellationToken = default)
    {
        return new SQLiteLockAwaitable(database, cancellationToken);
    }

    /// <summary>
    /// Async version of <see cref="SQLiteDatabase.ReadLock" />.
    /// </summary>
    public static Task<IDisposable> ReadLockAsync(this SQLiteDatabase database, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Task wait = database.WaitForActiveTransactionsAsync(cancellationToken);
        if (wait.IsCompletedSuccessfully)
        {
            return Task.FromResult(database.ReadLock());
        }

        return AwaitGateThenReadLock(wait);
    }

    /// <summary>
    /// Begins a transaction on the database asynchronously.
    /// </summary>
    /// <param name="database">The database to begin the transaction on.</param>
    /// <param name="separateConnection">
    /// When <see langword="true" />, the transaction runs on its own dedicated connection to the database file.
    /// All operations in the current async context are routed to that connection automatically,
    /// so standalone reads and writes on the shared connection are not blocked for the duration of the transaction.
    /// When <see langword="false" /> (the default), the transaction uses the shared connection
    /// and holds the exclusive write lock until it is committed or rolled back.
    /// </param>
    /// <param name="ct">Token that cancels the wait for the shared connection lock. Has no effect once the transaction has started.</param>
    public static SQLiteBeginTransactionAwaitable BeginTransactionAsync(this SQLiteDatabase database, bool separateConnection = false, CancellationToken ct = default)
    {
        return new SQLiteBeginTransactionAwaitable(database, separateConnection, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.Query<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.Query<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QueryFirst<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QueryFirst<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QueryFirstOrDefault<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QueryFirstOrDefault<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QuerySingle<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QuerySingle<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QuerySingleOrDefault<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.QuerySingleOrDefault<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.ExecuteScalar<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.ReadLockAsync(ct);
            return database.ExecuteScalar<T>(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            return database.Execute(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            return database.Execute(sql, parameters);
        }, ct);
    }

    /// <summary>
    /// Copies the database into <paramref name="destination" /> using SQLite's backup API.
    /// Acquires the connection lock on both databases asynchronously and runs the backup on the
    /// thread-pool worker that holds them.
    /// </summary>
    public static Task BackupToAsync(this SQLiteDatabase database, SQLiteDatabase destination, string sourceName = "main", string destName = "main", CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            using IDisposable __ = await destination.LockAsync(ct);
            database.BackupTo(destination, sourceName, destName);
        }, ct);
    }

    /// <summary>
    /// Copies the database into a new file at <paramref name="destinationPath" />.
    /// </summary>
    public static Task BackupToAsync(this SQLiteDatabase database, string destinationPath, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.BackupTo(destinationPath);
        }, ct);
    }

    /// <summary>
    /// Runs <c>VACUUM</c> on the main database, or on the attached <paramref name="schema" />.
    /// </summary>
    public static Task VacuumAsync(this SQLiteDatabase database, string? schema = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.Vacuum(schema);
        }, ct);
    }

    /// <summary>
    /// Runs <c>VACUUM INTO '...'</c> to write a clean copy of the database to
    /// <paramref name="destinationPath" />. Requires SQLite 3.27.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios13.0")]
#endif
    public static Task VacuumIntoAsync(this SQLiteDatabase database, string destinationPath, string? schema = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.VacuumInto(destinationPath, schema);
        }, ct);
    }

    /// <summary>
    /// Runs <c>REINDEX</c>, optionally limited to a table, index, or collation name.
    /// </summary>
    public static Task ReindexAsync(this SQLiteDatabase database, string? nameOrCollation = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.Reindex(nameOrCollation);
        }, ct);
    }

    /// <summary>
    /// Attaches another SQLite file to this connection.
    /// </summary>
    public static Task AttachDatabaseAsync(this SQLiteDatabase database, string path, string schemaName, string? encryptionKey = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.AttachDatabase(path, schemaName, encryptionKey);
        }, ct);
    }

    /// <summary>
    /// Detaches a previously attached database from this connection.
    /// </summary>
    public static Task DetachDatabaseAsync(this SQLiteDatabase database, string schemaName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await database.LockAsync(ct);
            database.DetachDatabase(schemaName);
        }, ct);
    }

    private static async Task<IDisposable> AwaitGateThenReadLock(Task wait)
    {
        await wait.ConfigureAwait(false);
        return NoOpLockObject.Instance;
    }
}
