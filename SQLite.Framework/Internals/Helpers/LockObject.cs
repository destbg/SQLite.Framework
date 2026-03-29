namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Used to lock the database connection for a specific operation.
/// </summary>
internal class LockObject : IDisposable
{
    private readonly SemaphoreSlim semaphore;
    private readonly AsyncLocal<bool> holdsLock;

    public LockObject(SemaphoreSlim semaphore, AsyncLocal<bool> holdsLock)
    {
        this.semaphore = semaphore;
        this.holdsLock = holdsLock;
        semaphore.Wait();
        holdsLock.Value = true;
    }

    public void Dispose()
    {
        holdsLock.Value = false;
        semaphore.Release();
    }
}