using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ScriptRows")]
public class ScriptRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class CountingCommandInterceptor : ISQLiteCommandInterceptor
{
    public int ExecutedCount { get; private set; }

    public void OnExecuting(SQLiteCommand command)
    {
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        ExecutedCount++;
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }

    public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
    {
    }
}

public class MigrationScriptTests
{
    [Fact]
    public void ScriptListsStatementsAndLeavesDatabaseUnchanged()
    {
        using TestDatabase db = new(useFile: true);
        bool callbackRan = false;

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m
                .RunBefore(_ => callbackRan = true)
                .CreateTable<ScriptRow>()
                .Insert(new ScriptRow { Id = 1, Name = "it's a name" })
                .Run(_ => callbackRan = true))
            .Script();

        Assert.Equal(
        [
            "-- run callback before schema changes at version 1",
            "CREATE TABLE IF NOT EXISTS \"ScriptRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)",
            "INSERT INTO \"ScriptRows\" (\"Id\", \"Name\") VALUES (1, 'it''s a name')",
            "-- run callback at version 1",
            "PRAGMA user_version = 1",
        ], statements);
        Assert.False(callbackRan);
        Assert.False(db.Schema.TableExists<ScriptRow>());
        Assert.Equal(0, db.Pragmas.UserVersion);
    }

    [Fact]
    public void ScriptListsRebuildStatements()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ScriptRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''))");
        db.Execute("INSERT INTO \"ScriptRows\" (\"Id\", \"Name\") VALUES (1, 'old')");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ScriptRow>(s => s.Set(x => x.Name, "new"), rebuild: true))
            .Script();

        Assert.Equal(
        [
            "PRAGMA foreign_keys = OFF",
            "PRAGMA defer_foreign_keys = ON",
            "CREATE TABLE \"ScriptRows__sqlitefw_migrate\" AS SELECT * FROM \"ScriptRows\"",
            "DROP TABLE \"ScriptRows\"",
            "CREATE TABLE \"ScriptRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)",
            "INSERT INTO \"ScriptRows\" (\"Id\", \"Name\") SELECT \"Id\", \"Name\" FROM \"ScriptRows__sqlitefw_migrate\"",
            "DROP TABLE \"ScriptRows__sqlitefw_migrate\"",
            "PRAGMA foreign_keys = 1",
            "PRAGMA defer_foreign_keys = 0",
            "UPDATE \"ScriptRows\" SET \"Name\" = 'new'",
            "PRAGMA user_version = 1",
        ], statements);
        Assert.Equal("old", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"ScriptRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ScriptReturnsEmptyWhenUpToDate()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 1;

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<ScriptRow>())
            .Script();

        Assert.Empty(statements);
    }

    [Fact]
    public void ScriptStopsCapturingAfterItReturns()
    {
        using TestDatabase db = new(useFile: true);

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<ScriptRow>())
            .Script();
        int captured = statements.Count;
        db.Execute("CREATE TABLE \"ScriptAfter\" (\"Id\" INTEGER PRIMARY KEY)");

        Assert.Equal(captured, statements.Count);
    }

    [Fact]
    public void ScriptFailureRollsBackAndRethrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.ThrowsAny<Exception>(() => db.Schema.Migrations()
            .Version(1, m => m.CreateTable<ScriptRow>().Sql("NOT A STATEMENT"))
            .Script());

        Assert.False(db.Schema.TableExists<ScriptRow>());
        Assert.Equal(0, db.Pragmas.UserVersion);
        Assert.Equal(1, db.Execute("CREATE TABLE \"ScriptAfterFailure\" (\"Id\" INTEGER PRIMARY KEY)") + 1);
    }

    [Fact]
    public void ScriptNotifiesRegisteredInterceptors()
    {
        CountingCommandInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor), useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<ScriptRow>())
            .Script();

        Assert.True(interceptor.ExecutedCount > 0);
    }

    [Fact]
    public async Task ScriptAsyncMatchesScript()
    {
        using TestDatabase db = new(useFile: true);

        IReadOnlyList<string> statements = await db.Schema.Migrations()
            .Version(1, m => m.CreateTable<ScriptRow>())
            .ScriptAsync();

        Assert.Equal(
        [
            "CREATE TABLE IF NOT EXISTS \"ScriptRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)",
            "PRAGMA user_version = 1",
        ], statements);
        Assert.False(db.Schema.TableExists<ScriptRow>());
    }
}
