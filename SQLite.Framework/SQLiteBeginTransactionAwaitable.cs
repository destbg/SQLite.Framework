namespace SQLite.Framework;

/// <summary>
/// An awaitable that begins a transaction while setting
/// the connection lock flag in the <em>caller's</em> execution context.
/// </summary>
public readonly struct SQLiteBeginTransactionAwaitable
{
    private readonly SQLiteDatabase database;
    private readonly bool separateConnection;

    internal SQLiteBeginTransactionAwaitable(SQLiteDatabase database, bool separateConnection)
    {
        this.database = database;
        this.separateConnection = separateConnection;
    }

    /// <summary>
    /// Returns the awaiter for this awaitable.
    /// </summary>
    public SQLiteBeginTransactionAwaiter GetAwaiter()
    {
        return new SQLiteBeginTransactionAwaiter(database, separateConnection);
    }
}