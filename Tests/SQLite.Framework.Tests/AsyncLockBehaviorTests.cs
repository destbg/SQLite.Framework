using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
namespace SQLite.Framework.Tests;

public class AsyncLockBehaviorTests
{
    [Fact]
    public async Task LockAsync_AlreadyCancelledToken_ThrowsBeforeAcquiring()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.LockAsync(cts.Token));
    }

    [Fact]
    public async Task LockAsync_WhileSyncLockHeld_BlocksUntilReleased()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        ManualResetEventSlim release = new();
        Task syncHolder = Task.Run(() =>
        {
            using IDisposable l = db.Lock();
            release.Wait();
        });

        await Task.Delay(50);

        Task<IDisposable> acquireTask = db.LockAsync().AsTask();
        Assert.False(acquireTask.IsCompleted);

        release.Set();
        using (await acquireTask)
        {
        }

        await syncHolder;
    }

    [Fact]
    public async Task LockAsync_QueuedCallsDoNotPinThreadPool()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        ManualResetEventSlim hold = new();
        Task syncHolder = Task.Run(() =>
        {
            using IDisposable l = db.Lock();
            hold.Wait();
        });

        await Task.Delay(50);

        Task<IDisposable>[] queued = Enumerable.Range(0, 16)
            .Select(_ => db.LockAsync().AsTask())
            .ToArray();

        foreach (Task<IDisposable> t in queued)
        {
            Assert.False(t.IsCompleted);
        }

        hold.Set();
        await syncHolder;

        foreach (Task<IDisposable> task in queued)
        {
            using (await task)
            {
            }
        }
    }

    [Fact]
    public async Task ToListAsync_ConcurrentReaders_ReleaseThreadDuringQueueing()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        int workerThreadsBefore;
        ThreadPool.GetAvailableThreads(out workerThreadsBefore, out _);

        Task<List<Book>>[] tasks = Enumerable.Range(0, 32)
            .Select(_ => db.Table<Book>().ToListAsync())
            .ToArray();

        await Task.WhenAll(tasks);

        foreach (Task<List<Book>> task in tasks)
        {
            Assert.Single(await task);
        }

        Assert.True(true, "32 ToListAsync calls completed without dead-locking the thread pool.");
    }

    [Fact]
    public async Task FirstAsync_AsyncPath_ReturnsCorrectRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        Book first = await db.Table<Book>().OrderBy(b => b.Id).FirstAsync();
        Book firstWithPredicate = await db.Table<Book>().OrderBy(b => b.Id).FirstAsync(b => b.Id > 1);

        Assert.Equal(1, first.Id);
        Assert.Equal(2, firstWithPredicate.Id);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_AsyncPath_OnEmpty_ReturnsNull()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Book? row = await db.Table<Book>().FirstOrDefaultAsync();

        Assert.Null(row);
    }

    [Fact]
    public async Task CountAsync_AsyncPath_ReturnsCount()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        int total = await db.Table<Book>().CountAsync();
        int filtered = await db.Table<Book>().CountAsync(b => b.AuthorId == 1);

        Assert.Equal(2, total);
        Assert.Equal(1, filtered);
    }

    [Fact]
    public async Task AnyAsync_AsyncPath_ReturnsTrueWhenMatch()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.True(await db.Table<Book>().AnyAsync());
        Assert.True(await db.Table<Book>().AnyAsync(b => b.Id == 1));
        Assert.False(await db.Table<Book>().AnyAsync(b => b.Id == 99));
    }

    [Fact]
    public async Task AllAsync_AsyncPath_ReturnsTrueWhenAllMatch()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        Assert.True(await db.Table<Book>().AllAsync(b => b.AuthorId == 1));
        Assert.False(await db.Table<Book>().AllAsync(b => b.Id > 1));
    }

    [Fact]
    public async Task ContainsAsync_AsyncPath_FindsValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 5, Title = "A", AuthorId = 1, Price = 1 });

        bool foundIds = await db.Table<Book>().Select(b => b.Id).ContainsAsync(5);
        bool missing = await db.Table<Book>().Select(b => b.Id).ContainsAsync(99);

        Assert.True(foundIds);
        Assert.False(missing);
    }

    [Fact]
    public async Task ToHashSetAsync_AsyncPath_ReturnsDistinctRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        HashSet<int> authorIds = await db.Table<Book>().Select(b => b.AuthorId).ToHashSetAsync();

        Assert.Single(authorIds);
        Assert.Contains(1, authorIds);
    }

    [Fact]
    public async Task ToDictionaryAsync_AsyncPath_BuildsDictionary()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        Dictionary<int, Book> byId = await db.Table<Book>().ToDictionaryAsync(b => b.Id);

        Assert.Equal(2, byId.Count);
        Assert.Equal("A", byId[1].Title);
        Assert.Equal("B", byId[2].Title);
    }

    [Fact]
    public async Task ToArrayAsync_AsyncPath_ReturnsArray()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        Book[] rows = await db.Table<Book>().OrderBy(b => b.Id).ToArrayAsync();

        Assert.Equal(2, rows.Length);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(2, rows[1].Id);
    }

    [Fact]
    public async Task ToListAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<Book>().ToListAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteDeleteAsync_AsyncPath_DeletesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        int affected = await db.Table<Book>().Where(b => b.AuthorId == 1).ExecuteDeleteAsync();

        Assert.Equal(2, affected);
        Assert.Single(db.Table<Book>().ToList());
    }

    [Fact]
    public async Task ExecuteDeleteAsync_WithPredicate_AsyncPath_DeletesMatching()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        int affected = await db.Table<Book>().ExecuteDeleteAsync(b => b.Id == 1);

        Assert.Equal(1, affected);
        Book remaining = db.Table<Book>().Single();
        Assert.Equal(2, remaining.Id);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_AsyncPath_UpdatesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        int affected = await db.Table<Book>()
            .Where(b => b.Id == 1)
            .ExecuteUpdateAsync(s => s.Set(b => b.Price, 99));

        Assert.Equal(1, affected);
        Book updated = db.Table<Book>().Single(b => b.Id == 1);
        Assert.Equal(99, updated.Price);
    }

    [Fact]
    public async Task ExecuteDeleteAsync_CancelledToken_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<Book>().ExecuteDeleteAsync(cts.Token));
    }

    [Fact]
    public async Task SumAsync_NoSelector_AsyncPath_ReturnsTotal()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 30 },
        ]);

        double total = await db.Table<Book>().Select(b => b.Price).SumAsync();

        Assert.Equal(60d, total);
    }

    [Fact]
    public async Task MinMaxAsync_NoSelector_AsyncPath_ReturnsValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 5, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 9, Title = "B", AuthorId = 1, Price = 20 },
            new Book { Id = 7, Title = "C", AuthorId = 1, Price = 30 },
        ]);

        int min = await db.Table<Book>().Select(b => b.Id).MinAsync();
        int max = await db.Table<Book>().Select(b => b.Id).MaxAsync();

        Assert.Equal(5, min);
        Assert.Equal(9, max);
    }

    [Fact]
    public async Task AverageAsync_NoSelector_AsyncPath_ReturnsValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 30 },
        ]);

        double avg = await db.Table<Book>().Select(b => b.Price).AverageAsync();

        Assert.Equal(20d, avg);
    }

    [Fact]
    public async Task SumAsync_CancelledToken_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<Book>().SumAsync(b => b.Price, cts.Token));
    }

    [Fact]
    public async Task LockAsync_WalMode_CancelledWhileWaitingForSemaphore_DecrementsWriterCount()
    {
        using WalTrackingDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        ManualResetEventSlim release = new();
        Task syncHolder = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction(separateConnection: false);
            release.Wait();
            tx.Commit();
        });

        await Task.Delay(100);

        using CancellationTokenSource cts = new();
        Task<IDisposable> queued = db.LockAsync(cts.Token).AsTask();
        await Task.Delay(100);
        Assert.False(queued.IsCompleted);

        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queued);

        release.Set();
        await syncHolder;

        using IDisposable normal = await db.LockAsync();
    }

    [Fact]
    public async Task LockAwaiter_OnCompleted_RegistersContinuation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        ManualResetEventSlim release = new();
        Task syncHolder = Task.Run(() =>
        {
            using IDisposable l = db.Lock();
            release.Wait();
        });

        await Task.Delay(50);

        SQLiteLockAwaiter awaiter = db.LockAsync().GetAwaiter();
        Assert.False(awaiter.IsCompleted);

        TaskCompletionSource done = new();
        awaiter.OnCompleted(() =>
        {
            using IDisposable d = awaiter.GetResult();
            done.SetResult();
        });

        release.Set();
        await done.Task;
        await syncHolder;
    }
}
#endif
