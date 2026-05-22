namespace SQLite.Framework;

/// <summary>
/// An awaitable that begins a transaction while setting
/// the connection lock flag in the <em>caller's</em> execution context.
/// </summary>
public readonly struct SQLiteBeginTransactionAwaitable
{
    private readonly SQLiteDatabase database;
    private readonly CancellationToken cancellationToken;

    internal SQLiteBeginTransactionAwaitable(SQLiteDatabase database, CancellationToken cancellationToken)
    {
        this.database = database;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Returns the awaiter for this awaitable.
    /// </summary>
    public SQLiteBeginTransactionAwaiter GetAwaiter()
    {
        return new SQLiteBeginTransactionAwaiter(database, cancellationToken);
    }
}
