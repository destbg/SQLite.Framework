using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ReturningLockRow
{
    [Key]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

public class ReturningWriteTransactionLockTests
{
    [Fact]
    public void ReturningAddWaitsForTheOpenTransaction()
    {
        using TestDatabase db = new();
        db.Table<ReturningLockRow>().Schema.CreateTable();

        using ManualResetEventSlim transactionStarted = new();
        ReturningLockRow? returned = null;

        Task writer = Task.Run(() =>
        {
            transactionStarted.Wait();
            returned = db.Table<ReturningLockRow>().Returning().Add(new ReturningLockRow { Id = 200, Text = "returning-add" });
        });

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            transactionStarted.Set();
            Thread.Sleep(300);
            transaction.Rollback();
        }

        writer.Wait();

        Assert.NotNull(returned);
        Assert.Equal(1, db.Table<ReturningLockRow>().Count(r => r.Id == 200));
    }
}
