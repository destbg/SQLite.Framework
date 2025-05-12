namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Used to lock the database connection for a specific operation.
/// </summary>
internal class LockObject : IDisposable
{
    private readonly object queryLock;

    public LockObject(object queryLock)
    {
        this.queryLock = queryLock;
        Monitor.Enter(queryLock);
    }

    public void Dispose()
    {
        Monitor.Exit(queryLock);
    }
}