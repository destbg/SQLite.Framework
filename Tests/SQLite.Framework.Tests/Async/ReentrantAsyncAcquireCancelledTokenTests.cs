using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReentrantAsyncAcquireCancelledTokenTests
{
    [Fact]
    public async Task BeginTransactionAsyncWithACancelledTokenThrowsWithNoTransactionOpen()
    {
        using TestDatabase db = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await using SQLiteTransaction transaction = await db.BeginTransactionAsync(cts.Token);
        });
    }

    [Fact]
    public async Task BeginTransactionAsyncWithACancelledTokenThrowsInsideAnOpenTransaction()
    {
        using TestDatabase db = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await using SQLiteTransaction outer = await db.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await using SQLiteTransaction nested = await db.BeginTransactionAsync(cts.Token);
        });
    }

    [Fact]
    public async Task LockAsyncWithACancelledTokenThrowsWhileTheSameFlowAlreadyHoldsTheLock()
    {
        using TestDatabase db = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        using IDisposable outer = await db.LockAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using IDisposable nested = await db.LockAsync(cts.Token);
        });
    }
}
