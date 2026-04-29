using System.Threading.Tasks;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TransactionAsyncProbe
{
    [Fact]
    public async Task SyncBegin_AsyncAdd_SyncCommit_Persists()
    {
        using TestDatabase db = NewDb();

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            await db.Table<Book>().AddAsync(NewBook(1), TestContext.Current.CancellationToken);
            tx.Commit();
        }

        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task SyncBegin_AsyncAdd_SyncRollback_DoesNotPersist()
    {
        using TestDatabase db = NewDb();

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            await db.Table<Book>().AddAsync(NewBook(1), TestContext.Current.CancellationToken);
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task SyncBeginSeparate_AsyncAdd_Rollback_DoesNotPersist()
    {
        using TestDatabase db = NewDb();

        using (SQLiteTransaction tx = db.BeginTransaction(separateConnection: true))
        {
            await db.Table<Book>().AddAsync(NewBook(1), TestContext.Current.CancellationToken);
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AsyncBegin_SyncAdd_SyncCommit_Persists()
    {
        using TestDatabase db = NewDb();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(ct: TestContext.Current.CancellationToken))
        {
            db.Table<Book>().Add(NewBook(1));
            tx.Commit();
        }

        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AsyncBegin_SyncAdd_Rollback_DoesNotPersist()
    {
        using TestDatabase db = NewDb();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(ct: TestContext.Current.CancellationToken))
        {
            db.Table<Book>().Add(NewBook(1));
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AsyncBeginSeparate_SyncAdd_Rollback_DoesNotPersist()
    {
        using TestDatabase db = NewDb();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken))
        {
            db.Table<Book>().Add(NewBook(1));
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AsyncBegin_AsyncAdd_AsyncCommit_Persists()
    {
        using TestDatabase db = NewDb();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(ct: TestContext.Current.CancellationToken))
        {
            await db.Table<Book>().AddAsync(NewBook(1), TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AsyncBegin_MixedSyncAndAsyncOps_AllInsideTransaction()
    {
        using TestDatabase db = NewDb();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(ct: TestContext.Current.CancellationToken))
        {
            db.Table<Book>().Add(NewBook(1));
            await db.Table<Book>().AddAsync(NewBook(2), TestContext.Current.CancellationToken);
            db.Table<Book>().Add(NewBook(3));
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task SyncBegin_MixedSyncAndAsyncOps_AllInsideTransaction()
    {
        using TestDatabase db = NewDb();

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Table<Book>().Add(NewBook(1));
            await db.Table<Book>().AddAsync(NewBook(2), TestContext.Current.CancellationToken);
            db.Table<Book>().Add(NewBook(3));
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    private static TestDatabase NewDb([System.Runtime.CompilerServices.CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
        return db;
    }

    private static Book NewBook(int id) => new()
    {
        Id = id,
        Title = "B" + id,
        AuthorId = 1,
        Price = id
    };
}
