using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25jReturningLockRows")]
public class H25jReturningLockRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Version { get; set; }
}

public sealed class H25jWriteLockStateInterceptor : ISQLiteCommandInterceptor
{
    public List<bool> WriteLockStates { get; } = [];

    public void OnExecuting(SQLiteCommand command)
    {
        if (command.CommandText.StartsWith("DELETE", StringComparison.Ordinal)
            || command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal))
        {
            WriteLockStates.Add(command.Database.HoldsConnectionLock);
        }
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }

    public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
    {
    }
}

public class ReturningQueryWriteConnectionLockTests
{
    [Fact]
    public void ADeleteWithAReturningProjectionHoldsTheConnectionLockLikeAPlainDelete()
    {
        H25jWriteLockStateInterceptor interceptor = new();
        using TestDatabase db = Seeded(interceptor);

        db.Table<H25jReturningLockRow>().Where(r => r.Id == 1).ExecuteDelete();
        db.Table<H25jReturningLockRow>().Where(r => r.Id == 2).Returning(r => r.Id).ExecuteDelete();

        Assert.Equal(2, interceptor.WriteLockStates.Count);
        Assert.Equal(interceptor.WriteLockStates[0], interceptor.WriteLockStates[1]);
    }

    [Fact]
    public void AnUpdateWithAReturningProjectionHoldsTheConnectionLockLikeAPlainUpdate()
    {
        H25jWriteLockStateInterceptor interceptor = new();
        using TestDatabase db = Seeded(interceptor);

        db.Table<H25jReturningLockRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Version, r => r.Version + 1));
        db.Table<H25jReturningLockRow>()
            .Where(r => r.Id == 2)
            .Returning(r => r.Id)
            .ExecuteUpdate(s => s.Set(r => r.Version, r => r.Version + 1));

        Assert.Equal(2, interceptor.WriteLockStates.Count);
        Assert.Equal(interceptor.WriteLockStates[0], interceptor.WriteLockStates[1]);
    }

    [Fact]
    public void ADeleteWithAReturningProjectionFromAnotherContextIsNotUndoneByATransactionRollback()
    {
        using TestDatabase db = Seeded(null);
        using ManualResetEventSlim transactionStarted = new();
        List<int> deleted = [];

        Task writer = Task.Run(() =>
        {
            transactionStarted.Wait(TimeSpan.FromSeconds(30));
            deleted = db.Table<H25jReturningLockRow>()
                .Where(r => r.Id == 1)
                .Returning(r => r.Id)
                .ExecuteDelete();
        });

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            transactionStarted.Set();
            Thread.Sleep(400);
            transaction.Rollback();
        }

        Assert.True(writer.Wait(TimeSpan.FromSeconds(30)));

        bool survived = db.Table<H25jReturningLockRow>().Any(r => r.Id == 1);

        Assert.Equal(deleted.Count == 1, !survived);
    }

    private static TestDatabase Seeded(H25jWriteLockStateInterceptor? interceptor)
    {
        TestDatabase db = interceptor == null
            ? new TestDatabase()
            : new TestDatabase(b => b.AddCommandInterceptor(interceptor));
        db.Table<H25jReturningLockRow>().Schema.CreateTable();
        db.Table<H25jReturningLockRow>().Add(new H25jReturningLockRow { Id = 1, Name = "a", Version = 1 });
        db.Table<H25jReturningLockRow>().Add(new H25jReturningLockRow { Id = 2, Name = "b", Version = 1 });
        return db;
    }
}
