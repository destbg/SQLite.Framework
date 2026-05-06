namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Disposable that releases the connection semaphore on the shared connection.
/// Used by <see cref="SQLiteLockAwaiter" /> for locks taken through the async path.
/// </summary>
internal sealed class ConnectionSemaphoreLockObject : IDisposable
{
    private readonly SQLiteDatabase database;

    public ConnectionSemaphoreLockObject(SQLiteDatabase database)
    {
        this.database = database;
    }

    public void Dispose()
    {
        database.ReleaseLock();
    }
}
