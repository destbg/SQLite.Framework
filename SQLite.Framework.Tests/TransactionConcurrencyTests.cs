using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class TransactionConcurrencyTests
{
    [Fact]
    public async Task EightConcurrentAsyncTransactions_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();

            await db.Table<Book>().AddAsync(new Book
            {
                Id = id,
                Title = $"Book {i}",
                AuthorId = 1,
                Price = id
            });

            Book? book = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            await db.Table<Book>().UpdateAsync(book);

            Book? updated = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);

            await tx.CommitAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task EightConcurrentAsyncTransactions_WithAddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];

            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);
            await tx.CommitAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task EightConcurrentSyncTransactions_WithAddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];

            using SQLiteTransaction tx = db.BeginTransaction();
            db.Table<Book>().AddRange(books, runInTransaction: false);
            tx.Commit();
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, db.Table<Book>().Count());
    }

    [Fact]
    public async Task EightConcurrentSyncTransactions_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            using SQLiteTransaction tx = db.BeginTransaction();

            db.Table<Book>().Add(new Book
            {
                Id = id,
                Title = $"Book {i}",
                AuthorId = 1,
                Price = id
            });

            Book? book = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            db.Table<Book>().Update(book);

            Book? updated = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);

            tx.Commit();
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, db.Table<Book>().Count());
    }

    [Fact]
    public async Task EightConcurrentSyncTransactions_NeverRunSimultaneously()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        int activeTransactions = 0;
        int maxActiveTransactions = 0;

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            using SQLiteTransaction tx = db.BeginTransaction();

            int current = Interlocked.Increment(ref activeTransactions);
            int snapshot;
            do
            {
                snapshot = maxActiveTransactions;
                if (current <= snapshot)
                {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref maxActiveTransactions, current, snapshot) != snapshot);

            db.Table<Book>().Add(new Book
            {
                Id = id,
                Title = $"Book {i}",
                AuthorId = 1,
                Price = id
            });
            db.Table<Book>().Update(new Book
            {
                Id = id,
                Title = $"Updated {i}",
                AuthorId = 1,
                Price = id
            });
            _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);

            Interlocked.Decrement(ref activeTransactions);
            tx.Commit();
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxActiveTransactions);
    }

    [Fact]
    public async Task EightConcurrentAsyncTransactions_NeverRunSimultaneously()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        int activeTransactions = 0;
        int maxActiveTransactions = 0;

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();

            int current = Interlocked.Increment(ref activeTransactions);
            int snapshot;
            do
            {
                snapshot = maxActiveTransactions;
                if (current <= snapshot)
                {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref maxActiveTransactions, current, snapshot) != snapshot);

            await db.Table<Book>().AddAsync(new Book
            {
                Id = id,
                Title = $"Book {i}",
                AuthorId = 1,
                Price = id
            });
            await db.Table<Book>().UpdateAsync(new Book
            {
                Id = id,
                Title = $"Updated {i}",
                AuthorId = 1,
                Price = id
            });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();

            Interlocked.Decrement(ref activeTransactions);
            await tx.CommitAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxActiveTransactions);
    }

    [Fact]
    public async Task ConcurrentTransactionAndAsyncQuery_DoNotInterleave()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 1,
            Price = 2
        });

        Task transactionTask = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            db.Table<Book>().Add(new Book
            {
                Id = 3,
                Title = "C",
                AuthorId = 1,
                Price = 3
            });
            tx.Commit();
        });

        Task queryTask = db.Table<Book>().ToListAsync();

        await Task.WhenAll(transactionTask, queryTask);

        List<Book> all = await db.Table<Book>().ToListAsync();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task AsyncTransaction_WithAwaitedQueriesInside_WorksCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        await using SQLiteTransaction tx = await db.BeginTransactionAsync();

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1
        });

        List<Book> mid = await db.Table<Book>().ToListAsync();
        Assert.Single(mid);

        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 1,
            Price = 2
        });

        await tx.CommitAsync();

        List<Book> all = await db.Table<Book>().ToListAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task AsyncTransaction_Rollback_RevertsChanges()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync())
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
            await tx.RollbackAsync();
        }

        List<Book> result = await db.Table<Book>().ToListAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task NestedTransactionViaAddRange_DoesNotDeadlock()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

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
            }
        ];

        await db.Table<Book>().AddRangeAsync(books);

        List<Book> result = await db.Table<Book>().ToListAsync();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void TransactionRollback_ReleasesLock_SoNextTransactionCanProceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        using (SQLiteTransaction tx = db.BeginTransaction())
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

        using (SQLiteTransaction tx2 = db.BeginTransaction())
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
    public void TransactionDispose_ReleasesLock_SoNextTransactionCanProceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        using (SQLiteTransaction _ = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "A",
                AuthorId = 1,
                Price = 1
            });
        }

        using (SQLiteTransaction tx2 = db.BeginTransaction())
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
}
