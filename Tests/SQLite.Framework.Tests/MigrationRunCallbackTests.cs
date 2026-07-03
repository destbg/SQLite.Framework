using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CallbackNote")]
public class CallbackNoteRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

[Table("CallbackDoc")]
public class CallbackDocRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

public class MigrationRunCallbackTests
{
    [Fact]
    public void RunReceivesTheDatabaseAndVersionRange()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteDatabase? seen = null;
        int fromVersion = -1;
        int targetVersion = -1;
        CancellationToken seenToken = new(canceled: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>())
            .Version(2, m => m.Run(ctx =>
            {
                seen = ctx.Database;
                fromVersion = ctx.FromVersion;
                targetVersion = ctx.TargetVersion;
                seenToken = ctx.CancellationToken;
            }))
            .Migrate();

        Assert.Same(db, seen);
        Assert.Equal(0, fromVersion);
        Assert.Equal(2, targetVersion);
        Assert.Equal(CancellationToken.None, seenToken);
    }

    [Fact]
    public void FromVersionReflectsTheRecordedVersion()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>())
            .Migrate();

        int fromVersion = -1;
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>())
            .Version(2, m => m.Run(ctx => fromVersion = ctx.FromVersion))
            .Migrate();

        Assert.Equal(1, fromVersion);
    }

    [Fact]
    public void RunBeforeRunsBeforeSchemaChangesAndRunAfter()
    {
        using TestDatabase db = new(useFile: true);
        List<string> events = [];

        db.Schema.Migrations()
            .Version(1, m => m
                .Run(ctx => events.Add($"run:{ctx.Database.Schema.TableExists<CallbackNoteRow>()}"))
                .RunBefore(ctx => events.Add($"before:{ctx.Database.Schema.TableExists<CallbackNoteRow>()}"))
                .CreateTable<CallbackNoteRow>())
            .Migrate();

        Assert.Equal(["before:False", "run:True"], events);
    }

    [Fact]
    public void CallbacksRunInVersionOrderWithinEachPhase()
    {
        using TestDatabase db = new(useFile: true);
        List<string> events = [];

        db.Schema.Migrations()
            .Version(2, m => m.RunBefore(ctx => events.Add("before2")).Run(ctx => events.Add("run2")))
            .Version(1, m => m.RunBefore(ctx => events.Add("before1")).Run(ctx => events.Add("run1")).CreateTable<CallbackNoteRow>())
            .Migrate();

        Assert.Equal(["before1", "before2", "run1", "run2"], events);
    }

    [Fact]
    public void RunExecutesInDeclaredOrderWithOtherDataSteps()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<CallbackNoteRow>()
                .Insert(new CallbackNoteRow { Text = "note" })
                .Run(ctx => ctx.Database.Execute("UPDATE \"CallbackNote\" SET \"Text\" = UPPER(\"Text\")"))
                .Sql("UPDATE \"CallbackNote\" SET \"Text\" = \"Text\" || '!'"))
            .Migrate();

        Assert.Equal("NOTE!", db.Table<CallbackNoteRow>().Single().Text);
    }

    [Fact]
    public void RunSeesTheFinalTableShape()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CallbackDoc\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"CallbackDoc\" (\"Id\") VALUES (1)");

        string? title = null;
        db.Schema.Migrations()
            .Version(1, m => m
                .TableChanged<CallbackDocRow>(s => s.Set(r => r.Title, "filled"))
                .Run(ctx => title = ctx.Database.Table<CallbackDocRow>().Single().Title))
            .Migrate();

        Assert.Equal("filled", title);
    }

    [Fact]
    public void RunBeforeReadsDataTheSchemaChangeDrops()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CallbackDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT NOT NULL DEFAULT '', \"Legacy\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"CallbackDoc\" (\"Id\", \"Legacy\") VALUES (1, 'one'), (2, 'two')");

        Dictionary<long, string> legacy = [];
        db.Schema.Migrations()
            .Version(1, m => m
                .RunBefore(ctx =>
                {
                    List<long> ids = ctx.Database.Query<long>("SELECT \"Id\" FROM \"CallbackDoc\" ORDER BY \"Id\"");
                    List<string> values = ctx.Database.Query<string>("SELECT \"Legacy\" FROM \"CallbackDoc\" ORDER BY \"Id\"");
                    for (int i = 0; i < ids.Count; i++)
                    {
                        legacy[ids[i]] = values[i];
                    }
                })
                .TableChanged<CallbackDocRow>()
                .Run(ctx =>
                {
                    foreach (CallbackDocRow doc in ctx.Database.Table<CallbackDocRow>().ToList())
                    {
                        doc.Title = legacy[doc.Id];
                        ctx.Database.Table<CallbackDocRow>().Update(doc);
                    }
                }))
            .Migrate();

        Assert.False(db.Schema.ColumnExists<CallbackDocRow>("Legacy"));
        Assert.Equal(["one", "two"], db.Table<CallbackDocRow>().OrderBy(d => d.Id).Select(d => d.Title).ToList());
    }

    [Fact]
    public void ThrowInRunRollsBackTheWholeRun()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Migrate();

        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Version(2, m => m
                .Sql("UPDATE \"CallbackNote\" SET \"Text\" = 'touched'")
                .Run(ctx => throw new InvalidOperationException("stop")));

        Assert.Throws<InvalidOperationException>(() => runner.Migrate());
        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Equal("note", db.Table<CallbackNoteRow>().Single().Text);
    }

    [Fact]
    public void ThrowInRunBeforeRollsBackItsOwnChanges()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Migrate();

        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Version(2, m => m.RunBefore(ctx =>
            {
                ctx.Database.Execute("INSERT INTO \"CallbackNote\" (\"Text\") VALUES ('extra')");
                throw new InvalidOperationException("stop");
            }));

        Assert.Throws<InvalidOperationException>(() => runner.Migrate());
        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Equal(["note"], db.Table<CallbackNoteRow>().Select(r => r.Text).ToList());
    }

    [Fact]
    public void MigrateReturnsZeroAndStampsTheVersionForCallbackOnlyRuns()
    {
        using TestDatabase db = new(useFile: true);
        bool ran = false;

        int count = db.Schema.Migrations()
            .Version(1, m => m.Run(ctx => ran = true))
            .Migrate();

        Assert.Equal(0, count);
        Assert.True(ran);
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task CallbacksUseTheSyncApiThroughMigrateAsync()
    {
        using TestDatabase db = new(useFile: true);
        int count = -1;

        await db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<CallbackNoteRow>()
                .Run(ctx =>
                {
                    ctx.Database.Table<CallbackNoteRow>().Add(new CallbackNoteRow { Text = "note" });
                    count = ctx.Database.Table<CallbackNoteRow>().Count();
                }))
            .MigrateAsync();

        Assert.Equal(1, count);
        Assert.Equal("note", db.Table<CallbackNoteRow>().Single().Text);
    }

    [Fact]
    public void PlanReportsCallbacksWithoutRunningThem()
    {
        using TestDatabase db = new(useFile: true);
        bool ran = false;

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m
                .RunBefore(ctx => ran = true)
                .Run(ctx => ran = true))
            .Version(2, m => m
                .RunAsync(ctx =>
                {
                    ran = true;
                    return Task.CompletedTask;
                })
                .RunBeforeAsync(ctx =>
                {
                    ran = true;
                    return Task.CompletedTask;
                }))
            .Plan();

        Assert.False(plan.IsUpToDate);
        Assert.Equal([
            "run callback before schema changes at version 1",
            "run callback at version 1",
            "run async callback at version 2",
            "run async callback before schema changes at version 2",
        ], plan.Operations);
        Assert.False(ran);
    }

    [Fact]
    public async Task RunAsyncAwaitsAsyncDatabaseCalls()
    {
        using TestDatabase db = new(useFile: true);
        int seenCount = -1;

        await db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<CallbackNoteRow>()
                .RunAsync(async ctx =>
                {
                    await ctx.Database.Table<CallbackNoteRow>().AddAsync(new CallbackNoteRow { Text = "note" }, ctx.CancellationToken);
                    List<CallbackNoteRow> rows = await ctx.Database.Table<CallbackNoteRow>().ToListAsync(ctx.CancellationToken);
                    seenCount = rows.Count;
                }))
            .MigrateAsync();

        Assert.Equal(1, seenCount);
        Assert.Equal("note", db.Table<CallbackNoteRow>().Single().Text);
    }

    [Fact]
    public async Task RunBeforeAsyncReadsTheOldShape()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CallbackDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT NOT NULL DEFAULT '', \"Legacy\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"CallbackDoc\" (\"Id\", \"Legacy\") VALUES (1, 'one')");

        string? legacy = null;
        await db.Schema.Migrations()
            .Version(1, m => m
                .RunBeforeAsync(async ctx =>
                {
                    List<string> values = await ctx.Database.QueryAsync<string>("SELECT \"Legacy\" FROM \"CallbackDoc\"", [], ctx.CancellationToken);
                    legacy = values[0];
                })
                .TableChanged<CallbackDocRow>())
            .MigrateAsync();

        Assert.Equal("one", legacy);
        Assert.False(db.Schema.ColumnExists<CallbackDocRow>("Legacy"));
    }

    [Fact]
    public async Task MixedCallbacksRunInDeclaredOrderWithinEachPhase()
    {
        using TestDatabase db = new(useFile: true);
        List<string> events = [];

        await db.Schema.Migrations()
            .Version(1, m => m
                .RunBefore(ctx => events.Add("before1"))
                .RunBeforeAsync(ctx =>
                {
                    events.Add("before2");
                    return Task.CompletedTask;
                })
                .CreateTable<CallbackNoteRow>()
                .Run(ctx => events.Add("run1"))
                .RunAsync(ctx =>
                {
                    events.Add("run2");
                    return Task.CompletedTask;
                }))
            .MigrateAsync();

        Assert.Equal(["before1", "before2", "run1", "run2"], events);
    }

    [Fact]
    public void MigrateThrowsWhenAPendingVersionDeclaresAnAsyncCallback()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<CallbackNoteRow>()
                .RunAsync(ctx => Task.CompletedTask));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => runner.Migrate());
        Assert.Equal("A pending version declares an async callback. Call MigrateAsync instead of Migrate.", ex.Message);
        Assert.False(db.Schema.TableExists<CallbackNoteRow>());
        Assert.Equal(0, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task MigrateAsyncDoesNothingWhenUpToDate()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>())
            .Migrate();

        bool ran = false;
        int count = await db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Run(ctx => ran = true))
            .MigrateAsync();

        Assert.Equal(0, count);
        Assert.False(ran);
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task TheContextCarriesTheCancellationToken()
    {
        using TestDatabase db = new(useFile: true);
        using CancellationTokenSource cts = new();
        CancellationToken seen = default;

        await db.Schema.Migrations()
            .Version(1, m => m.RunAsync(ctx =>
            {
                seen = ctx.CancellationToken;
                return Task.CompletedTask;
            }))
            .MigrateAsync(cts.Token);

        Assert.Equal(cts.Token, seen);
    }

    [Fact]
    public async Task ThrowInRunAsyncRollsBackTheWholeRun()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Migrate();

        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<CallbackNoteRow>().Insert(new CallbackNoteRow { Text = "note" }))
            .Version(2, m => m
                .Sql("UPDATE \"CallbackNote\" SET \"Text\" = 'touched'")
                .RunAsync(ctx => throw new InvalidOperationException("stop")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.MigrateAsync());
        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Equal("note", db.Table<CallbackNoteRow>().Single().Text);
    }

    [Fact]
    public void NullRunAsyncActionThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.RunAsync(null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void NullRunBeforeAsyncActionThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.RunBeforeAsync(null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void NullRunActionThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.Run(null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void NullRunBeforeActionThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.RunBefore(null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

}
