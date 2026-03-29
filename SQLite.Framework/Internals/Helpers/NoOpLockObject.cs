namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// A no-op lock used when the current execution context already holds the connection lock.
/// </summary>
internal sealed class NoOpLockObject : IDisposable
{
    public static readonly NoOpLockObject Instance = new();

    private NoOpLockObject() { }

    public void Dispose() { }
}