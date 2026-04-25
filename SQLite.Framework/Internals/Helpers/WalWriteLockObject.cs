namespace SQLite.Framework.Internals.Helpers;

internal sealed class WalWriteLockObject : IDisposable
{
    private readonly SQLiteDatabase database;

    public WalWriteLockObject(SQLiteDatabase database)
    {
        this.database = database;
    }

    public void Dispose()
    {
        database.ReleaseWalWrite();
    }
}
