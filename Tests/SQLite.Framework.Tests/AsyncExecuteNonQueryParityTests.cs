using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class AsyncPairRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

internal sealed class RecordingCommandInterceptor : ISQLiteCommandInterceptor
{
    public List<string> Events { get; } = [];

    public void OnExecuting(SQLiteCommand command)
    {
        Events.Add("Executing");
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        Events.Add("Executed");
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
        Events.Add("Failed");
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }
}

public class AsyncExecuteNonQueryParityTests
{
    private const string TwoInsertSql =
        "INSERT INTO \"AsyncPairRow\" (\"Id\", \"Amount\") VALUES (1, 10); INSERT INTO \"AsyncPairRow\" (\"Id\", \"Amount\") VALUES (2, 20)";

    [Fact]
    public async Task ExecuteNonQueryAsyncRunsEveryStatement()
    {
        using TestDatabase db = new();
        db.Table<AsyncPairRow>().Schema.CreateTable();

        int syncChanges = db.CreateCommand(TwoInsertSql, []).ExecuteNonQuery();
        int syncCount = db.Table<AsyncPairRow>().Count();

        Assert.Equal(2, syncChanges);
        Assert.Equal(2, syncCount);

        db.Table<AsyncPairRow>().Clear();

        int asyncChanges = await db.CreateCommand(TwoInsertSql, []).ExecuteNonQueryAsync();
        int asyncCount = db.Table<AsyncPairRow>().Count();

        Assert.Equal(syncChanges, asyncChanges);
        Assert.Equal(syncCount, asyncCount);
    }

    [Fact]
    public async Task ExecuteNonQueryAsyncNotifiesInterceptors()
    {
        RecordingCommandInterceptor recorder = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(recorder));
        db.Table<AsyncPairRow>().Schema.CreateTable();

        recorder.Events.Clear();
        db.CreateCommand("INSERT INTO \"AsyncPairRow\" (\"Id\", \"Amount\") VALUES (1, 1)", []).ExecuteNonQuery();

        Assert.Equal(["Executing", "Executed"], recorder.Events);

        recorder.Events.Clear();
        await db.CreateCommand("INSERT INTO \"AsyncPairRow\" (\"Id\", \"Amount\") VALUES (2, 2)", []).ExecuteNonQueryAsync();

        Assert.Equal(["Executing", "Executed"], recorder.Events);
    }

    [Fact]
    public async Task ExecuteWithLastRowIdAsyncNotifiesInterceptors()
    {
        RecordingCommandInterceptor recorder = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(recorder));
        db.Table<AsyncPairRow>().Schema.CreateTable();

        recorder.Events.Clear();
        await db.CreateCommand("INSERT INTO \"AsyncPairRow\" (\"Id\", \"Amount\") VALUES (3, 3)", []).ExecuteWithLastRowIdAsync();

        Assert.Equal(["Executing", "Executed"], recorder.Events);
    }
}
