using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class RecordingInterceptor : ISQLiteCommandInterceptor
{
    public List<string> Executing { get; } = [];
    public List<string> Executed { get; } = [];
    public List<string> Failed { get; } = [];

    public void OnExecuting(SQLiteCommand command) => Executing.Add(command.CommandText);
    public void OnExecuted(SQLiteCommand command, int? rowsAffected) => Executed.Add(command.CommandText);
    public void OnFailed(SQLiteCommand command, Exception exception) => Failed.Add(command.CommandText);
}

public class TransactionAndAsyncLifecycleTests
{
    [Fact]
    public async Task ExecuteReaderAsyncFiresInterceptors()
    {
        RecordingInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        interceptor.Executing.Clear();
        interceptor.Executed.Clear();

        SQLiteCommand command = db.CreateCommand("SELECT * FROM Books", []);
        using (SQLiteDataReader reader = await command.ExecuteReaderAsync())
        {
            while (reader.Read())
            {
            }
        }

        Assert.NotEmpty(interceptor.Executing);
        Assert.NotEmpty(interceptor.Executed);
    }

    [Fact]
    public async Task WalLockAsyncDisposeClearsReentrancyFlag()
    {
        using TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        using (IDisposable l = await db.LockAsync())
        {
        }

        Assert.False(db.HoldsConnectionLock);
    }

    [Fact]
    public void CommitFailureReleasesConnectionLock()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE Parent (Id INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE Child (Id INTEGER PRIMARY KEY, ParentId INTEGER REFERENCES Parent(Id) DEFERRABLE INITIALLY DEFERRED)");
        db.Execute("PRAGMA foreign_keys = ON");

        SQLiteTransaction tx = db.BeginTransaction();
        db.Execute("INSERT INTO Child (Id, ParentId) VALUES (1, 999)");

        Assert.ThrowsAny<Exception>(() => tx.Commit());

        Assert.False(db.HoldsConnectionLock);
    }
}
