using System;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class SavepointBlockingInterceptor : ISQLiteCommandInterceptor
{
    public void OnExecuting(SQLiteCommand command)
    {
        if (command.CommandText.StartsWith("SAVEPOINT", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("savepoint blocked");
        }
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }
}

public class BeginTransactionFailureLockTests
{
    [Fact]
    public void BeginTransactionFailureReleasesConnectionLock()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new SavepointBlockingInterceptor()));

        Assert.ThrowsAny<Exception>(() => db.BeginTransaction());

        Assert.False(db.HoldsConnectionLock);
    }
}
