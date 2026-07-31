using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H26sCommandOutcomeInterceptor : ISQLiteCommandInterceptor
{
    public List<long> Executing { get; } = [];

    public List<long> Executed { get; } = [];

    public List<long> Failed { get; } = [];

    public void OnExecuting(SQLiteCommand command)
    {
        Executing.Add(command.Id);
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        Executed.Add(command.Id);
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
        Failed.Add(command.Id);
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }

    public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
    {
    }
}

public class FailedRowStepCommandOutcomeTests
{
    private const string OverflowingSql = "SELECT ABS(-9223372036854775807 - 1)";

    [Fact]
    public void ACommandThatFailsWhileSteppingReportsASingleOutcome()
    {
        H26sCommandOutcomeInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor));

        Assert.Throws<SQLiteException>(() => db.ExecuteScalar<long>(OverflowingSql));

        long id = interceptor.Executing[^1];
        int outcomes = interceptor.Executed.Count(x => x == id) + interceptor.Failed.Count(x => x == id);

        Assert.Equal(1, outcomes);
    }

    [Fact]
    public void ACommandThatFailsWhileSteppingWritesASingleLogLine()
    {
        List<string> lines = [];
        using TestDatabase db = new(b => b.LogCommands(lines.Add));

        Assert.Throws<SQLiteException>(() => db.ExecuteScalar<long>(OverflowingSql));

        Assert.Single(lines, line => line.Contains(OverflowingSql, StringComparison.Ordinal));
    }
}
