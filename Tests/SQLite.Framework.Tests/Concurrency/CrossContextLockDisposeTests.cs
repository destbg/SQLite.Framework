using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CrossContextLockDisposeTests
{
    [Fact]
    public void WritesStillWaitAfterABlobStreamIsDisposedOnAnotherThread()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"BlobHolderRows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB)");
        db.Execute("INSERT INTO \"BlobHolderRows\" (\"Id\", \"Data\") VALUES (1, zeroblob(4))");

        SQLiteBlobStream stream = db.OpenBlobStream("BlobHolderRows", "Data", 1, writable: true);
        Task.Run(() => stream.Dispose()).Wait(TimeSpan.FromSeconds(5));

        using ManualResetEventSlim taken = new();
        bool holderReleased = false;
        Task holder = Task.Run(() =>
        {
            using (db.Lock())
            {
                taken.Set();
                Thread.Sleep(700);
                holderReleased = true;
            }
        });

        Assert.True(taken.Wait(TimeSpan.FromSeconds(5)));
        db.Execute("INSERT INTO \"BlobHolderRows\" (\"Id\", \"Data\") VALUES (2, zeroblob(1))");
        bool releasedBeforeWrite = holderReleased;
        Assert.True(holder.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(releasedBeforeWrite);
    }
}
