namespace SQLite.Framework.Internals.Helpers;

internal sealed class WalWriteLockObject(SQLiteDatabase database) : IDisposable
{
    public void Dispose()
    {
        database.ReleaseWalWrite();
    }
}
