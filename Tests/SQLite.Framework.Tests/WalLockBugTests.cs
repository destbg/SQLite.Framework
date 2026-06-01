using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WalLockBugTests
{
    [Fact]
    public void SyncLockThenBeginTransactionInWalModeDoesNotDeadlock()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        Exception? error = null;
        Thread thread = new(() =>
        {
            try
            {
                using IDisposable held = db.Lock();
                using SQLiteTransaction tx = db.BeginTransaction();
                tx.Commit();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
        };
        thread.Start();
        bool completed = thread.Join(TimeSpan.FromSeconds(5));

        Assert.True(completed);
        Assert.Null(error);
    }
}
