namespace SQLite.Framework;

/// <summary>
/// Awaiter for <see cref="SQLiteLockAwaitable" />.
/// </summary>
/// <remarks>
/// <see cref="GetResult" /> runs right after the await and sets the re-entrancy flag
/// in the caller's flow, so later <c>Lock()</c> calls become no-ops.
/// </remarks>
public readonly struct SQLiteLockAwaiter : ICriticalNotifyCompletion
{
    private readonly SQLiteDatabase database;
    private readonly Task acquireTask;
    private readonly bool isReentrant;
    private readonly bool isWal;

    internal SQLiteLockAwaiter(SQLiteDatabase database, CancellationToken cancellationToken)
    {
        this.database = database;

        if (database.HoldsConnectionLock)
        {
            isReentrant = true;
            isWal = false;
            acquireTask = Task.CompletedTask;
            return;
        }

        isReentrant = false;
        isWal = database.Options.IsWalMode;
        acquireTask = isWal
            ? database.AcquireWalWriteAsync(cancellationToken)
            : database.WaitConnectionSemaphoreAsync(cancellationToken);
    }

    /// <summary>
    /// Indicates that the lock has been acquired.
    /// </summary>
    public bool IsCompleted => acquireTask.IsCompleted;

    /// <inheritdoc />
    public void OnCompleted(Action continuation)
    {
        acquireTask.GetAwaiter().OnCompleted(continuation);
    }

    /// <inheritdoc />
    public void UnsafeOnCompleted(Action continuation)
    {
        acquireTask.GetAwaiter().UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// Returns the lock disposable.
    /// Disposing it releases the lock and clears the re-entrancy flag.
    /// </summary>
    /// <remarks>
    /// Runs in the caller's execution context so the flag is visible to code after the await.
    /// </remarks>
    public IDisposable GetResult()
    {
        acquireTask.GetAwaiter().GetResult();

        if (isReentrant)
        {
            return NoOpLockObject.Instance;
        }

        database.SetConnectionLock();

        return isWal
            ? new WalWriteLockObject(database)
            : new ConnectionSemaphoreLockObject(database);
    }
}
