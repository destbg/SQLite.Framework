using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HkBlobLockRows")]
public class HkBlobLockRow
{
    [Key]
    public long Id { get; set; }

    public byte[]? Data { get; set; }

    public int Num { get; set; }
}

public class BlobStreamAsyncOpenWriteParityTests
{
    [Fact]
    public void WriteAfterSyncBlobOpenCompletes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<HkBlobLockRow>();
        db.Table<HkBlobLockRow>().Add(new HkBlobLockRow { Id = 1, Data = [1, 2, 3, 4], Num = 0 });

        SQLiteBlobStream stream = db.OpenBlobStream("HkBlobLockRows", "Data", 1);
        Task<int> update = Task.Run(() => db.Execute("UPDATE \"HkBlobLockRows\" SET \"Num\" = 5 WHERE \"Id\" = 1"));
        bool completed = update.Wait(TimeSpan.FromSeconds(2));
        stream.Dispose();
        int changed = update.Result;

        Assert.True(completed);
        Assert.Equal(1, changed);
        Assert.Equal(5, db.Table<HkBlobLockRow>().Single(r => r.Id == 1).Num);
    }

    [Fact]
    public async Task WriteAfterAsyncBlobOpenCompletes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<HkBlobLockRow>();
        db.Table<HkBlobLockRow>().Add(new HkBlobLockRow { Id = 2, Data = [1, 2, 3, 4], Num = 0 });

        SQLiteBlobStream stream = await db.OpenBlobStreamAsync("HkBlobLockRows", "Data", 2);
        Task<int> update = Task.Factory.StartNew(
            () => db.Execute("UPDATE \"HkBlobLockRows\" SET \"Num\" = 5 WHERE \"Id\" = 2"),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        bool completed = update.Wait(TimeSpan.FromSeconds(10));
        stream.Dispose();
        int changed = await update;

        Assert.True(completed);
        Assert.Equal(1, changed);
        Assert.Equal(5, db.Table<HkBlobLockRow>().Single(r => r.Id == 2).Num);
    }

    [Fact]
    public async Task EntityOverloadWriteAfterAsyncBlobOpenCompletes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<HkBlobLockRow>();
        db.Table<HkBlobLockRow>().Add(new HkBlobLockRow { Id = 3, Data = [9, 8, 7], Num = 0 });

        SQLiteBlobStream stream = await db.OpenBlobStreamAsync<HkBlobLockRow>(3, r => r.Data);
        Task<int> update = Task.Factory.StartNew(
            () => db.Table<HkBlobLockRow>().Update(new HkBlobLockRow { Id = 3, Data = [9, 8, 7], Num = 6 }),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        bool completed = update.Wait(TimeSpan.FromSeconds(10));
        stream.Dispose();
        int changed = await update;

        Assert.True(completed);
        Assert.Equal(1, changed);
        Assert.Equal(6, db.Table<HkBlobLockRow>().Single(r => r.Id == 3).Num);
    }
}
