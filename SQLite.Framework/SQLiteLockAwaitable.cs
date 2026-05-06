namespace SQLite.Framework;

/// <summary>
/// Awaitable that acquires the connection lock and sets the re-entrancy flag
/// in the caller's execution context, so later sync calls to
/// <see cref="SQLiteDatabase.Lock" /> stay re-entrant.
/// </summary>
public readonly struct SQLiteLockAwaitable
{
    private readonly SQLiteDatabase database;
    private readonly CancellationToken cancellationToken;

    internal SQLiteLockAwaitable(SQLiteDatabase database, CancellationToken cancellationToken)
    {
        this.database = database;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Returns the awaiter for this awaitable.
    /// </summary>
    public SQLiteLockAwaiter GetAwaiter()
    {
        return new SQLiteLockAwaiter(database, cancellationToken);
    }

    /// <summary>
    /// Wraps this awaitable in a <see cref="Task{TResult}" />.
    /// Use this when you need to store the in-flight acquisition or check its state.
    /// </summary>
    public Task<IDisposable> AsTask()
    {
        SQLiteLockAwaitable self = this;
        return Run();

        async Task<IDisposable> Run()
        {
            return await self;
        }
    }
}
