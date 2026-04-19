using System.Runtime.CompilerServices;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class WalConcurrencyTests
{
    [Fact]
    public void WalMode_PragmaIsSet_AfterConnectionOpen()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        string? mode = db.ExecuteScalar<string>("PRAGMA journal_mode");
        Assert.Equal("wal", mode);
    }

    [Fact]
    public async Task NonWalMode_WritesAreStillSerialized()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            db.Table<Book>().Update(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task WalMode_EightSyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

            Book? book = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            db.Table<Book>().Update(book);

            Book? updated = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, db.Table<Book>().Count());
    }

    [Fact]
    public async Task WalMode_EightAsyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

            Book? book = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            await db.Table<Book>().UpdateAsync(book);

            Book? updated = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task WalMode_EightMixedTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        List<Task> tasks = [];

        for (int i = 0; i < 8; i++)
        {
            int id = i + 1;
            string title = $"Book {i}";
            string updated = $"Updated {i}";

            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = 1, Price = id });

                    Book? book = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                    Assert.NotNull(book);
                    Assert.Equal(title, book.Title);

                    book.Title = updated;
                    db.Table<Book>().Update(book);

                    Book? result = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                    Assert.NotNull(result);
                    Assert.Equal(updated, result.Title);
                }));
            }
            else
            {
                tasks.Add(RunAsync(id, title, updated));
            }
        }

        await Task.WhenAll(tasks);

        Assert.Equal(8, db.Table<Book>().Count());

        async Task RunAsync(int id, string title, string updated)
        {
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = title, AuthorId = 1, Price = id });

            Book? book = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(book);
            Assert.Equal(title, book.Title);

            book.Title = updated;
            await db.Table<Book>().UpdateAsync(book);

            Book? result = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(result);
            Assert.Equal(updated, result.Title);
        }
    }

    [Fact]
    public async Task WalMode_EightSyncTasks_AddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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
            db.Table<Book>().AddRange(books);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, db.Table<Book>().Count());
    }

    [Fact]
    public async Task WalMode_EightAsyncTasks_AddRangeAsync_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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
            await db.Table<Book>().AddRangeAsync(books);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task WalMode_ConcurrentWrites_AreNotSerialized()
    {
        MultiCoreFact.SkipIfInsufficientCores();
        using WalTrackingDatabase db = new();
        db.LockHoldMilliseconds = 50;
        db.Table<Book>().CreateTable();

        Barrier barrier = new(8);

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            barrier.SignalAndWait();
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(db.MaxConcurrentLockHolders > 1,
            $"Expected WAL writes to overlap, but peak concurrent holders was {db.MaxConcurrentLockHolders}.");
        Assert.Equal(8, db.Table<Book>().Count());
    }

    [Fact]
    public async Task WalMode_EightAsyncTasks_WritesAreNotSerialized()
    {
        MultiCoreFact.SkipIfInsufficientCores();
        using WalTrackingDatabase db = new();
        db.LockHoldMilliseconds = 50;
        db.Table<Book>().CreateTable();

        Barrier barrier = new(8);

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await Task.Run(() => barrier.SignalAndWait());
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(db.MaxConcurrentLockHolders > 1,
            $"Expected WAL writes to overlap, but peak concurrent holders was {db.MaxConcurrentLockHolders}.");
        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task WalMode_EightMixedTasks_WritesAreNotSerialized()
    {
        MultiCoreFact.SkipIfInsufficientCores();
        using WalTrackingDatabase db = new();
        db.LockHoldMilliseconds = 50;
        db.Table<Book>().CreateTable();

        Barrier barrier = new(8);
        List<Task> tasks = [];

        for (int i = 0; i < 8; i++)
        {
            int id = i + 1;
            string title = $"Book {i}";

            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = 1, Price = id });
                }));
            }
            else
            {
                tasks.Add(Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    await db.Table<Book>().AddAsync(new Book { Id = id, Title = title, AuthorId = 1, Price = id });
                }));
            }
        }

        await Task.WhenAll(tasks);

        Assert.True(db.MaxConcurrentLockHolders > 1,
            $"Expected WAL writes to overlap, but peak concurrent holders was {db.MaxConcurrentLockHolders}.");
        Assert.Equal(8, db.Table<Book>().Count());
    }

    [Fact]
    public async Task WalMode_EightSyncTasks_ConcurrentReads_AllReturnCorrectData()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        Barrier barrier = new(8);

        Task[] tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait();
            List<Book> books = db.Table<Book>().ToList();
            Assert.Equal(8, books.Count);
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task WalMode_EightAsyncTasks_ConcurrentReads_AllReturnCorrectData()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        TaskCompletionSource gate = new();
        int arrivals = 0;

        Task[] tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            if (Interlocked.Increment(ref arrivals) == 8)
            {
                gate.SetResult();
            }

            await gate.Task;

            List<Book> books = await db.Table<Book>().ToListAsync();
            Assert.Equal(8, books.Count);
        }).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task WalMode_ConcurrentReadsAndWrites_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        Task[] background = Enumerable.Range(0, 6).Select(i => Task.Run(async () =>
        {
            for (int round = 0; round < 5; round++)
            {
                _ = await db.Table<Book>().ToListAsync();
                await db.Table<Book>().UpdateAsync(
                    new Book { Id = i + 1, Title = $"Background {i} round {round}", AuthorId = 1, Price = i + 1 });
            }
        })).ToArray();

        Task[] foreground = Enumerable.Range(6, 2).Select(i => Task.Run(async () =>
        {
            await db.Table<Book>().UpdateAsync(
                new Book { Id = i + 1, Title = $"Foreground {i}", AuthorId = 1, Price = i + 1 });
        })).ToArray();

        await Task.WhenAll([.. background, .. foreground]);

        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task WalMode_Write_CompletesWhileBackgroundReadIsInProgress()
    {
        using ManualResetEventSlim readStarted = new(false);
        using ManualResetEventSlim releaseRead = new(false);
        using HoldableReadDatabase db = new(readStarted, releaseRead);
        db.IsWalMode = true;

        db.Table<Book>().CreateTable();

        for (int i = 0; i < 5; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        Task readTask = Task.Run(() => db.Table<Book>().ToList());

        readStarted.Wait();

        await db.Table<Book>().UpdateAsync(new Book { Id = 1, Title = "Updated", AuthorId = 1, Price = 1 });

        releaseRead.Set();
        await readTask;

        Book result = db.Table<Book>().First(b => b.Id == 1);
        Assert.Equal("Updated", result.Title);
    }

    [Fact]
    public async Task WalMode_EightConcurrentWrites_AllPersist()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task WalMode_BeginTransaction_WaitsForActiveWalWrite()
    {
        using ManualResetEventSlim writeAcquired = new(false);
        using ManualResetEventSlim releaseWrite = new(false);
        using WalWriteHoldingDatabase db = new(writeAcquired, releaseWrite);
        db.Table<Book>().CreateTable();

        db.ArmHold();

        Task writeTask = Task.Run(() =>
            db.Table<Book>().Add(new Book { Id = 99, Title = "Held", AuthorId = 1, Price = 99 }));

        writeAcquired.Wait();

        bool txStarted = false;
        Task txTask = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            txStarted = true;
            tx.Commit();
        });

        await Task.Delay(50);
        Assert.False(txStarted);
        Assert.False(txTask.IsCompleted);

        releaseWrite.Set();
        await writeTask;
        await txTask;

        Assert.True(txStarted);
    }

    [Fact]
    public async Task WalMode_WritesWaitForActiveTransaction()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        TaskCompletionSource txStarted = new();
        SemaphoreSlim txRelease = new(0, 1);

        Task txTask = Task.Run(async () =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            txStarted.SetResult();
            await txRelease.WaitAsync();
            tx.Commit();
        });

        await txStarted.Task;

        bool writeCompleted = false;
        Task writeTask = Task.Run(async () =>
        {
            await db.Table<Book>().AddAsync(new Book { Id = 1, Title = "Late write", AuthorId = 1, Price = 1 });
            writeCompleted = true;
        });

        await Task.Delay(50);
        Assert.False(writeCompleted);

        txRelease.Release();
        await txTask;
        await writeTask;

        Assert.True(writeCompleted);
        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task WalMode_EightConcurrentSyncTransactions_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            using SQLiteTransaction tx = db.BeginTransaction();

            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

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
    public async Task WalMode_EightConcurrentAsyncTransactions_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();

            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

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
    public async Task WalMode_EightConcurrentSyncTransactions_WithAddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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
    public async Task WalMode_EightConcurrentAsyncTransactions_WithAddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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
    public async Task WalMode_EightConcurrentSyncTransactions_NeverRunSimultaneously()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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

            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            db.Table<Book>().Update(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);

            Interlocked.Decrement(ref activeTransactions);
            tx.Commit();
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxActiveTransactions);
    }

    [Fact]
    public async Task WalMode_EightConcurrentAsyncTransactions_NeverRunSimultaneously()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
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

            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            await db.Table<Book>().UpdateAsync(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();

            Interlocked.Decrement(ref activeTransactions);
            await tx.CommitAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxActiveTransactions);
    }

    [Fact]
    public async Task WalMode_ConcurrentTransactionAndAsyncQuery_DoNotInterleave()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });

        Task transactionTask = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            db.Table<Book>().Add(new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 });
            tx.Commit();
        });

        Task queryTask = db.Table<Book>().ToListAsync();

        await Task.WhenAll(transactionTask, queryTask);

        List<Book> all = await db.Table<Book>().ToListAsync();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task WalMode_AsyncTransaction_WithAwaitedQueriesInside_WorksCorrectly()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        await using SQLiteTransaction tx = await db.BeginTransactionAsync();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        List<Book> mid = await db.Table<Book>().ToListAsync();
        Assert.Single(mid);

        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });

        await tx.CommitAsync();

        List<Book> all = await db.Table<Book>().ToListAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task WalMode_AsyncTransaction_Rollback_RevertsChanges()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        await using (SQLiteTransaction tx = await db.BeginTransactionAsync())
        {
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
            await tx.RollbackAsync();
        }

        List<Book> result = await db.Table<Book>().ToListAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task WalMode_NestedTransactionViaAddRange_DoesNotDeadlock()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        List<Book> books =
        [
            new() { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new() { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new() { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ];

        await db.Table<Book>().AddRangeAsync(books);

        List<Book> result = await db.Table<Book>().ToListAsync();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void WalMode_TransactionRollback_ReleasesLock_SoNextTransactionCanProceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
            tx.Rollback();
        }

        using (SQLiteTransaction tx2 = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            tx2.Commit();
        }

        List<Book> result = db.Table<Book>().ToList();
        Assert.Single(result);
        Assert.Equal("B", result[0].Title);
    }

    [Fact]
    public void WalMode_TransactionDispose_ReleasesLock_SoNextTransactionCanProceed()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        using (SQLiteTransaction _ = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        }

        using (SQLiteTransaction tx2 = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            tx2.Commit();
        }

        List<Book> result = db.Table<Book>().ToList();
        Assert.Single(result);
        Assert.Equal("B", result[0].Title);
    }

    [Fact]
    public async Task WalMode_Read_CompletesWhileTransactionHoldsWriteLock()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 5; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        TaskCompletionSource txStarted = new();
        SemaphoreSlim release = new(0, 1);

        Task txTask = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            txStarted.SetResult();
            await release.WaitAsync();
            await tx.CommitAsync();
        });

        await txStarted.Task;

        List<Book> books = await db.Table<Book>().ToListAsync();
        Assert.Equal(5, books.Count);
        Assert.False(txTask.IsCompleted);

        release.Release();
        await txTask;
    }

    [Fact]
    public async Task WalMode_Read_DoesNotWaitBehindQueuedTransaction()
    {
        using TestDatabase db = new();
        db.IsWalMode = true;
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 5; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        TaskCompletionSource txStarted = new();
        SemaphoreSlim release = new(0, 1);

        Task txTask = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            txStarted.SetResult();
            await release.WaitAsync();
            await tx.CommitAsync();
        });

        await txStarted.Task;

        Task queuedTxTask = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
        });

        await Task.Delay(30);

        List<Book> books = await db.Table<Book>().ToListAsync();
        Assert.Equal(5, books.Count);
        Assert.False(queuedTxTask.IsCompleted);

        release.Release();
        await txTask;
        await queuedTxTask;
    }
}

file static class MultiCoreFact
{
    public static void SkipIfInsufficientCores(int minimumCores = 4)
    {
        if (Environment.ProcessorCount < minimumCores)
        {
            Assert.Skip($"Requires at least {minimumCores} logical processors (machine has {Environment.ProcessorCount}).");
        }
    }
}

file sealed class WalWriteHoldingDatabase : TestDatabase
{
    private readonly ManualResetEventSlim writeAcquired;
    private readonly ManualResetEventSlim releaseWrite;
    private int armed;

    public WalWriteHoldingDatabase(
        ManualResetEventSlim writeAcquired,
        ManualResetEventSlim releaseWrite,
        [CallerMemberName] string? methodName = null)
        : base(methodName)
    {
        IsWalMode = true;
        this.writeAcquired = writeAcquired;
        this.releaseWrite = releaseWrite;
    }

    public void ArmHold() => Interlocked.Exchange(ref armed, 1);

    public override IDisposable Lock()
    {
        IDisposable inner = base.Lock();

        if (Interlocked.Exchange(ref armed, 0) == 1)
        {
            writeAcquired.Set();
            return new HeldWriteLock(inner, releaseWrite);
        }

        return inner;
    }

    private sealed class HeldWriteLock(IDisposable inner, ManualResetEventSlim release) : IDisposable
    {
        public void Dispose()
        {
            release.Wait();
            inner.Dispose();
        }
    }
}

file sealed class HoldableReadDatabase : TestDatabase
{
    private readonly ManualResetEventSlim readStarted;
    private readonly ManualResetEventSlim releaseRead;

    public HoldableReadDatabase(
        ManualResetEventSlim readStarted,
        ManualResetEventSlim releaseRead,
        [CallerMemberName] string? methodName = null)
        : base(methodName)
    {
        this.readStarted = readStarted;
        this.releaseRead = releaseRead;
    }

    public override IDisposable ReadLock()
    {
        readStarted.Set();
        return new HeldReadLock(releaseRead);
    }

    private sealed class HeldReadLock(ManualResetEventSlim release) : IDisposable
    {
        public void Dispose() => release.Wait();
    }
}
