namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Disposable that releases the connection semaphore on the shared connection.
/// Used by <see cref="SQLiteLockAwaiter" /> for locks taken through the async path.
/// </summary>
internal sealed class ConnectionSemaphoreLockObject : IDisposable
{
    private readonly SQLiteDatabase database;
    private readonly LockToken token;
    private bool disposed;

    public ConnectionSemaphoreLockObject(SQLiteDatabase database, LockToken token)
    {
        this.database = database;
        this.token = token;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.ReleaseLock(token);
    }
}
