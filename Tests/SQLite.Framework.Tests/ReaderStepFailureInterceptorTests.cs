using SQLite.Framework;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReaderStepFailureInterceptorTests
{
    [Fact]
    public void ScalarStepFailureNotifiesFailedInterceptor()
    {
        StepRecordingInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor));

        Assert.Throws<SQLiteException>(() => db.ExecuteScalar<long>("SELECT ABS(-9223372036854775807 - 1)"));

        Assert.Equal(1, interceptor.FailedCount);
    }

    [Fact]
    public void QueryStepFailureNotifiesFailedInterceptor()
    {
        StepRecordingInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor));

        Assert.Throws<SQLiteException>(() => db.Query<long>("SELECT ABS(-9223372036854775807 - 1)"));

        Assert.Equal(1, interceptor.FailedCount);
    }

    [Fact]
    public void StepFailureLogsFailedLine()
    {
        List<string> lines = [];
        using TestDatabase db = new(b => b.LogCommands(lines.Add));

        Assert.Throws<SQLiteException>(() => db.ExecuteScalar<long>("SELECT ABS(-9223372036854775807 - 1)"));

        Assert.Contains(lines, line => line.Contains("FAILED", StringComparison.Ordinal));
    }

    private sealed class StepRecordingInterceptor : ISQLiteCommandInterceptor
    {
        public int FailedCount { get; private set; }

        public void OnExecuting(SQLiteCommand command)
        {
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
            FailedCount++;
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }
}
