using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaMigrateAsyncTests
{
    [Fact]
    public async Task SchemaMigrateAsyncGenericCreatesMissingTable()
    {
        using TestDatabase db = new();

        await db.Schema.MigrateAsync<MigSimple>(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<MigSimple>());
    }

    [Fact]
    public async Task SchemaMigrateAsyncWithFillFillsColumns()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        await db.Schema.MigrateAsync<MigFill>(
            m => m.Set(x => x.Status, "active").Set(x => x.Doubled, x => x.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal("active", db.Table<MigFill>().Single().Status);
    }

    [Fact]
    public async Task SchemaMigrateByRebuildAsyncGenericCreatesMissingTable()
    {
        using TestDatabase db = new();

        await db.Schema.MigrateByRebuildAsync<MigSimple>(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<MigSimple>());
    }

    [Fact]
    public async Task SchemaMigrateByRebuildAsyncWithFillFillsColumns()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        await db.Schema.MigrateByRebuildAsync<MigFill>(
            m => m.Set(x => x.Status, "active").Set(x => x.Doubled, x => x.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal("active", db.Table<MigFill>().Single().Status);
    }
}
