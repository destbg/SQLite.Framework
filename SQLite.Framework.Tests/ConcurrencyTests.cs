using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task EightSyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
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
    public async Task EightSyncTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            db.Table<Book>().Update(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightAsyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
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
    public async Task EightAsyncTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            await db.Table<Book>().UpdateAsync(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightMixedTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
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
    public async Task EightSyncTasks_AddRange_AllSucceed()
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
            db.Table<Book>().AddRange(books);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, db.Table<Book>().Count());
    }

    [Fact]
    public async Task EightSyncTasks_AddRange_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
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

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightAsyncTasks_AddRangeAsync_AllSucceed()
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
            await db.Table<Book>().AddRangeAsync(books);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task EightAsyncTasks_AddRangeAsync_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
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

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightSyncTasks_ConcurrentReads_AllReturnCorrectData()
    {
        using TestDatabase db = new();
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
    public async Task EightAsyncTasks_ConcurrentReads_AllReturnCorrectData()
    {
        using TestDatabase db = new();
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

    [MultiCoreFact]
    public async Task EightSyncTasks_ConcurrentReads_CanHoldReadLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        Barrier barrier = new(8);

        Task[] tasks = Enumerable.Range(0, 8).Select(n => Task.Run(() =>
        {
            barrier.SignalAndWait();
            List<Book> books = db.Table<Book>().ToList();
            Assert.Equal(8, books.Count);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(db.MaxConcurrentReadHolders > 1,
            $"Expected multiple concurrent readers but peak was {db.MaxConcurrentReadHolders}.");
    }

    [MultiCoreFact]
    public async Task EightAsyncTasks_ConcurrentReads_CanHoldReadLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        for (int i = 0; i < 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 1, Price = i + 1 });
        }

        Barrier barrier = new(8);

        Task[] tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            List<Book> books = await db.Table<Book>().ToListAsync();
            Assert.Equal(8, books.Count);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(db.MaxConcurrentReadHolders > 1,
            $"Expected multiple concurrent readers but peak was {db.MaxConcurrentReadHolders}.");
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_AllSucceed()
    {
        using TestDatabase db = new();
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
    public async Task EightMixedTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
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
                    db.Table<Book>().Update(new Book { Id = id, Title = updated, AuthorId = 1, Price = id });
                    _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                }));
            }
            else
            {
                tasks.Add(RunAsync(id, title, updated));
            }
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);

        async Task RunAsync(int id, string title, string updated)
        {
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = title, AuthorId = 1, Price = id });
            await db.Table<Book>().UpdateAsync(new Book { Id = id, Title = updated, AuthorId = 1, Price = id });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
        }
    }

    [Fact]
    public async Task Write_CompletesWhileBackgroundReadIsInProgress()
    {
        using ManualResetEventSlim readStarted = new(false);
        using ManualResetEventSlim releaseRead = new(false);
        using HoldableReadDatabase db = new(readStarted, releaseRead);

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
}

[AttributeUsage(AttributeTargets.Method)]
file sealed class MultiCoreFactAttribute : FactAttribute
{
    public MultiCoreFactAttribute(int minimumCores = 4)
    {
        if (Environment.ProcessorCount < minimumCores)
        {
            Skip = $"Requires at least {minimumCores} logical processors (machine has {Environment.ProcessorCount}).";
        }
    }
}

file sealed class HoldableReadDatabase : TestDatabase
{
    private readonly ManualResetEventSlim readStarted;
    private readonly ManualResetEventSlim releaseRead;

    public HoldableReadDatabase(ManualResetEventSlim readStarted, ManualResetEventSlim releaseRead,
        [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null)
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
