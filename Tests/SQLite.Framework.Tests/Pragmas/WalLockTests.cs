using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WalLockTests
{
    [Fact]
    public void SyncLockThenBeginTransactionInWalModeCommitsAndPersists()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        bool completed = RunWithTimeout(() =>
        {
            using IDisposable held = db.Lock();
            using SQLiteTransaction tx = db.BeginTransaction();
            db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
            tx.Commit();
        }, out Exception? error);

        Assert.True(completed);
        Assert.Null(error);
        Assert.Single(db.Table<Book>().ToList());
        db.Dispose();
    }

    [Fact]
    public void SyncLockReentrantInWalModeCompletes()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        bool completed = RunWithTimeout(() =>
        {
            using IDisposable outer = db.Lock();
            using IDisposable inner = db.Lock();
            db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        }, out Exception? error);

        Assert.True(completed);
        Assert.Null(error);
        Assert.Single(db.Table<Book>().ToList());
        db.Dispose();
    }

    [Fact]
    public void SyncLockInWalModeReleasesForNextWriter()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        bool completed = RunWithTimeout(() =>
        {
            using (db.Lock())
            {
                db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
            }

            db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

            using (db.Lock())
            {
                db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });
            }
        }, out Exception? error);

        Assert.True(completed);
        Assert.Null(error);
        Assert.Equal(3, db.Table<Book>().ToList().Count);
        db.Dispose();
    }

    [Fact]
    public void SyncLockThenRolledBackTransactionInWalModeReleasesAndDiscards()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        bool completed = RunWithTimeout(() =>
        {
            using (db.Lock())
            {
                using SQLiteTransaction tx = db.BeginTransaction();
                db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
                tx.Rollback();
            }

            db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        }, out Exception? error);

        Assert.True(completed);
        Assert.Null(error);
        List<int> ids = db.Table<Book>().Select(b => b.Id).OrderBy(i => i).ToList();
        Assert.Equal(new[] { 2 }, ids);
        db.Dispose();
    }

    [Fact]
    public void AsyncLockThenBeginTransactionInWalModeStillCommits()
    {
        TestDatabase db = new(b => b.UseWalMode());
        db.Table<Book>().Schema.CreateTable();

        bool completed = Task.Run(async () =>
        {
            using (await db.LockAsync())
            {
                await using SQLiteTransaction tx = await db.BeginTransactionAsync();
                db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
                await tx.CommitAsync();
            }
        }).Wait(TimeSpan.FromSeconds(5));

        Assert.True(completed);
        Assert.Single(db.Table<Book>().ToList());
        db.Dispose();
    }

    private static bool RunWithTimeout(Action action, out Exception? error)
    {
        Exception? captured = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        })
        {
            IsBackground = true,
        };
        thread.Start();
        bool completed = thread.Join(TimeSpan.FromSeconds(5));
        error = captured;
        return completed;
    }
}
