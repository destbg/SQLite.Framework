using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class SeparateConnectionTransactionTests
{
    [Fact]
    public void Commit_PersistsChanges()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using (SQLiteTransaction tx = db.BeginTransaction(separateConnection: true))
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
            tx.Commit();
        }

        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public void Rollback_RevertsChanges()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using (SQLiteTransaction tx = db.BeginTransaction(separateConnection: true))
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
            tx.Rollback();
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public void Dispose_WithoutCommit_AutoRollsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using (SQLiteTransaction _ = db.BeginTransaction(separateConnection: true))
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
        }

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public void AfterRollback_NextTransactionCanProceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using (SQLiteTransaction tx = db.BeginTransaction(separateConnection: true))
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
            tx.Rollback();
        }

        using (SQLiteTransaction tx2 = db.BeginTransaction(separateConnection: true))
        {
            db.Table<Book>().Add(new Book
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            });
            tx2.Commit();
        }

        List<Book> result = db.Table<Book>().ToList();
        Assert.Single(result);
        Assert.Equal("B", result[0].Title);
    }

    [Fact]
    public void ReadWithinTransaction_SeesUncommittedData()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using SQLiteTransaction tx = db.BeginTransaction(separateConnection: true);

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1
        });
        List<Book> mid = db.Table<Book>().ToList();
        Assert.Single(mid);

        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 1,
            Price = 2
        });
        tx.Commit();

        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public async Task Async_ReadWithinTransaction_SeesUncommittedData()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        await using SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken);

        await db.Table<Book>().AddAsync(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1
        }, TestContext.Current.CancellationToken);
        List<Book> mid = await db.Table<Book>().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(mid);

        await db.Table<Book>().AddAsync(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 1,
            Price = 2
        }, TestContext.Current.CancellationToken);
        await tx.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MainConnectionRead_NotBlockedWhileTransactionIsOpen()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        for (int i = 0; i < 5; i++)
        {
            db.Table<Book>().Add(new Book
            {
                Id = i + 1,
                Title = $"Book {i}",
                AuthorId = 1,
                Price = i + 1
            });
        }

        TaskCompletionSource txStarted = new();
        SemaphoreSlim release = new(0, 1);

        Task txTask = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true);
            txStarted.SetResult();
            await release.WaitAsync(TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        await txStarted.Task;

        List<Book> books = await db.Table<Book>().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(5, books.Count);
        Assert.False(txTask.IsCompleted);

        release.Release();
        await txTask;
    }

    [Fact]
    public async Task DoesNotBlockOtherSeparateConnectionTransactions()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SemaphoreSlim release = new(0, 1);
        TaskCompletionSource tx1Started = new();

        Task tx1Task = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true);
            tx1Started.SetResult();
            await release.WaitAsync(TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        await tx1Started.Task;

        await using SQLiteTransaction tx2 = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken);
        Assert.False(tx1Task.IsCompleted);
        await tx2.CommitAsync(TestContext.Current.CancellationToken);

        release.Release();
        await tx1Task;
    }

    [Fact]
    public async Task Sync_DoesNotBlockOtherSeparateConnectionTransactions()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SemaphoreSlim release = new(0, 1);
        TaskCompletionSource tx1Started = new();

        Task tx1Task = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction(separateConnection: true);
            tx1Started.SetResult();
            release.Wait(TestContext.Current.CancellationToken);
            tx.Commit();
        }, TestContext.Current.CancellationToken);

        await tx1Started.Task;

        using SQLiteTransaction tx2 = db.BeginTransaction(separateConnection: true);
        Assert.False(tx1Task.IsCompleted);
        tx2.Commit();

        release.Release();
        await tx1Task;
    }

    [Fact]
    public async Task Async_Commit_PersistsChanges()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken))
        {
            await db.Table<Book>().AddAsync(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            }, TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Async_Rollback_RevertsChanges()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken))
        {
            await db.Table<Book>().AddAsync(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            }, TestContext.Current.CancellationToken);
            await tx.RollbackAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Async_Dispose_WithoutCommit_AutoRollsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        await using (SQLiteTransaction _ = await db.BeginTransactionAsync(separateConnection: true, ct: TestContext.Current.CancellationToken))
        {
            await db.Table<Book>().AddAsync(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            }, TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AddRange_WithSeparateConnection_CommitsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
            new()
            {
                Id = 3,
                Title = "C",
                AuthorId = 1,
                Price = 3
            },
        ];

        db.Table<Book>().AddRange(books, separateConnection: true);

        Assert.Equal(3, db.Table<Book>().Count());
    }

    [Fact]
    public void UpdateRange_WithSeparateConnection_CommitsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
        ];

        db.Table<Book>().AddRange(books);

        List<Book> updated = books.Select(b => new Book
        {
            Id = b.Id,
            Title = $"Updated {b.Title}",
            AuthorId = b.AuthorId,
            Price = b.Price
        }).ToList();
        db.Table<Book>().UpdateRange(updated, separateConnection: true);

        List<Book> result = db.Table<Book>().ToList();
        Assert.All(result, b => Assert.StartsWith("Updated", b.Title));
    }

    [Fact]
    public void RemoveRange_WithSeparateConnection_RemovesAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
        ];

        db.Table<Book>().AddRange(books);
        db.Table<Book>().RemoveRange(books, separateConnection: true);

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AddRangeAsync_WithSeparateConnection_CommitsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
            new()
            {
                Id = 3,
                Title = "C",
                AuthorId = 1,
                Price = 3
            },
        ];

        await db.Table<Book>().AddRangeAsync(books, separateConnection: true, ct: TestContext.Current.CancellationToken);

        Assert.Equal(3, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateRangeAsync_WithSeparateConnection_CommitsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
        ];

        await db.Table<Book>().AddRangeAsync(books, ct: TestContext.Current.CancellationToken);

        List<Book> updated = books.Select(b => new Book
        {
            Id = b.Id,
            Title = $"Updated {b.Title}",
            AuthorId = b.AuthorId,
            Price = b.Price
        }).ToList();
        await db.Table<Book>().UpdateRangeAsync(updated, separateConnection: true, ct: TestContext.Current.CancellationToken);

        List<Book> result = await db.Table<Book>().ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(result, b => Assert.StartsWith("Updated", b.Title));
    }

    [Fact]
    public async Task RemoveRangeAsync_WithSeparateConnection_RemovesAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<Book> books =
        [
            new()
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            },
            new()
            {
                Id = 2,
                Title = "B",
                AuthorId = 1,
                Price = 2
            },
        ];

        await db.Table<Book>().AddRangeAsync(books, ct: TestContext.Current.CancellationToken);
        await db.Table<Book>().RemoveRangeAsync(books, separateConnection: true, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, await db.Table<Book>().CountAsync(TestContext.Current.CancellationToken));
    }
}