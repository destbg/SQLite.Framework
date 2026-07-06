using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LockLeaseDoubleDisposeTests
{
    [Fact]
    public void SyncLockLeaseSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.OpenConnection();

        IDisposable lease = db.Lock();
        lease.Dispose();
        lease.Dispose();

        using (db.Lock())
        {
            Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
        }
    }

    [Fact]
    public async Task AsyncLockLeaseSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.OpenConnection();

        IDisposable lease = await db.LockAsync();
        lease.Dispose();
        lease.Dispose();

        using (db.Lock())
        {
            Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
        }
    }
}
