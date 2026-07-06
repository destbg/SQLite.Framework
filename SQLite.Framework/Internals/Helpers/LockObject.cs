namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Used to lock the database connection for a specific operation.
/// </summary>
internal class LockObject : IDisposable
{
    private readonly SQLiteDatabase database;
    private readonly LockToken token;
    private bool disposed;

    public LockObject(SQLiteDatabase database, SemaphoreSlim semaphore)
    {
        this.database = database;
        semaphore.Wait();
        token = database.SetConnectionLock();
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
