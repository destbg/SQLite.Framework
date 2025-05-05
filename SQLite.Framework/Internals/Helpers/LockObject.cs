namespace SQLite.Framework.Internals.Helpers;

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