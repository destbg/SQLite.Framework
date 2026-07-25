using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AsyncLockTaskReentrancyTests
{
    [Fact]
    public void LockTakenThroughTheAwaitableStaysReentrant()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"LockProbeRows\" (\"Id\" INTEGER PRIMARY KEY)");

        Task worker = Task.Run(async () =>
        {
            using (await db.LockAsync())
            {
                db.Execute("INSERT INTO \"LockProbeRows\" (\"Id\") VALUES (1)");
            }
        });

        Assert.True(worker.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"LockProbeRows\""));
    }
}
