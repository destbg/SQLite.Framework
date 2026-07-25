using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlockReadsDuringTransactionTests
{
    [Fact]
    public void OptionDefault_IsFalse()
    {
        using TestDatabase db = new();
        Assert.False(db.Options.BlockReadsDuringTransaction);
    }

    [Fact]
    public void OptionEnabled_RoundTripsThroughBuilder()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.UseBlockReadsDuringTransaction();
        SQLiteOptions options = builder.Build();
        Assert.True(options.BlockReadsDuringTransaction);
    }

    [Fact]
    public void ReadFromTransactionContext_NotBlocked()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();

        using SQLiteTransaction tx = db.BeginTransaction();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        int count = db.Table<Book>().Count();
        Assert.Equal(1, count);
        tx.Commit();
    }

    [Fact]
    public async Task AsyncRead_OptionEnabled_NoTransaction_DoesNotBlock()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        int count = await db.Table<Book>().CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AsyncRead_FromTransactionContext_NotBlocked()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();

        await using SQLiteTransaction tx = await db.BeginTransactionAsync();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        int count = await db.Table<Book>().CountAsync();
        Assert.Equal(1, count);
        await tx.CommitAsync();
    }

    [Fact]
    public async Task ReadLockAsync_GateTaskIsHeldUntilCommit()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Seed", AuthorId = 1, Price = 1 });

        using SemaphoreSlim insideTransaction = new(0, 1);
        using SemaphoreSlim allowCommit = new(0, 1);

        Task writer = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            insideTransaction.Release();
            await allowCommit.WaitAsync();
            await tx.CommitAsync();
        });

        await insideTransaction.WaitAsync();

        Task<IDisposable> readLockTask = db.ReadLockAsync();
        Assert.False(readLockTask.IsCompleted);

        allowCommit.Release();
        await writer;

        using IDisposable readLock = await readLockTask;
        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public async Task ReadLockAsync_GateTaskIsHeldUntilRollback()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Seed", AuthorId = 1, Price = 1 });

        using SemaphoreSlim insideTransaction = new(0, 1);
        using SemaphoreSlim allowRollback = new(0, 1);

        Task writer = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            insideTransaction.Release();
            await allowRollback.WaitAsync();
            await tx.RollbackAsync();
        });

        await insideTransaction.WaitAsync();

        Task<IDisposable> readLockTask = db.ReadLockAsync();
        Assert.False(readLockTask.IsCompleted);

        allowRollback.Release();
        await writer;

        using IDisposable readLock = await readLockTask;
        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task ReadLockAsync_CancellationFaultsTheGateTask()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();

        using SemaphoreSlim insideTransaction = new(0, 1);
        using SemaphoreSlim allowCommit = new(0, 1);

        Task writer = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
            insideTransaction.Release();
            await allowCommit.WaitAsync();
            await tx.CommitAsync();
        });

        await insideTransaction.WaitAsync();

        using CancellationTokenSource cts = new();
        Task<IDisposable> readLockTask = db.ReadLockAsync(cts.Token);
        Assert.False(readLockTask.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => readLockTask);

        allowCommit.Release();
        await writer;
    }

    [Fact]
    public async Task SeparateConnectionTransaction_OptionDisabled_DoesNotBlock()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Seed", AuthorId = 1, Price = 1 });

        using SemaphoreSlim insideTransaction = new(0, 1);
        using SemaphoreSlim allowCommit = new(0, 1);

        Task writer = Task.Run(async () =>
        {
            await using SQLiteTransaction tx = await db.BeginTransactionAsync();
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            insideTransaction.Release();
            await allowCommit.WaitAsync();
            await tx.CommitAsync();
        });

        await insideTransaction.WaitAsync();

        Task<IDisposable> readLockTask = db.ReadLockAsync();
        Assert.True(readLockTask.IsCompletedSuccessfully);
        readLockTask.Result.Dispose();

        allowCommit.Release();
        await writer;
    }

    [Fact]
    public async Task SequentialTransactions_GateResetsBetween()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        db.Table<Book>().Schema.CreateTable();

        await using (SQLiteTransaction tx1 = await db.BeginTransactionAsync())
        {
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
            await tx1.CommitAsync();
        }

        Task<IDisposable> firstReadLock = db.ReadLockAsync();
        Assert.True(firstReadLock.IsCompletedSuccessfully);
        firstReadLock.Result.Dispose();

        await using (SQLiteTransaction tx2 = await db.BeginTransactionAsync())
        {
            db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
            await tx2.CommitAsync();
        }

        Task<IDisposable> secondReadLock = db.ReadLockAsync();
        Assert.True(secondReadLock.IsCompletedSuccessfully);
        secondReadLock.Result.Dispose();

        Assert.Equal(2, db.Table<Book>().Count());
    }
}
