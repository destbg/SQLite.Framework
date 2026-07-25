using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrationRunnerCreateThenChangeTests
{
    [Fact]
    public void CreateTableWithIndexThenTableChanged_SameRun_IndexSurvives()
    {
        using ModelTestDatabase db = new(model => model.Entity<MigIndexed>().Index(m => m.Name, name: "IX_MigIndexed_Name"));

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigIndexed>())
            .Version(2, m => m.TableChanged<MigIndexed>())
            .Migrate();

        Assert.True(db.Schema.IndexExists("IX_MigIndexed_Name"));
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void CreateTableWithComputedThenTableChanged_SameRun_ComputesCorrectly()
    {
        using ModelTestDatabase db = new(model => model.Entity<MigComputed>().Computed(m => m.Total, m => m.Price * m.Quantity));

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigComputed>())
            .Version(2, m => m.TableChanged<MigComputed>())
            .Migrate();

        db.Execute("INSERT INTO \"MigComputed\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 2, 3)");
        Assert.Equal(6, db.Table<MigComputed>().Single().Total);
    }

    [Fact]
    public void CreateTableWithTriggerThenTableChanged_SameRun_TriggerStillFires()
    {
        using MigrateTriggerDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigAudit>().CreateTable<MigSimple>())
            .Version(2, m => m.TableChanged<MigSimple>())
            .Migrate();

        db.Table<MigSimple>().Add(new MigSimple { Id = 1, Name = "a" });

        Assert.Single(db.Table<MigAudit>().ToList());
    }

    [Fact]
    public void CreateFtsTableThenTableChanged_SameRun_RemainsSearchable()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SimpleSearchEntity>())
            .Version(2, m => m.TableChanged<SimpleSearchEntity>())
            .Migrate();

        Assert.True(db.Schema.TableExists<SimpleSearchEntity>());
        Assert.Empty(db.Table<SimpleSearchEntity>()
            .Where(s => SQLiteFTS5Functions.Match(s, "native"))
            .ToList());
    }

    [Fact]
    public void CreateRTreeTableThenTableChanged_SameRun_QueryWorks()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<Region2D>())
            .Version(2, m => m.TableChanged<Region2D>())
            .Migrate();

        db.Table<Region2D>().Add(new Region2D { Id = 1, MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 });
        db.Table<Region2D>().Add(new Region2D { Id = 2, MinX = 100, MaxX = 110, MinY = 100, MaxY = 110 });

        List<Region2D> hits = db.Table<Region2D>()
            .Where(r => r.MinX <= 5 && r.MaxX >= 5 && r.MinY <= 5 && r.MaxY >= 5)
            .ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
    }

    [Fact]
    public void CreateTableThenTableChanged_SameVersion_Skips()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>().TableChanged<RunnerDataRow>(b => b.Set(s => s.Value, 0)))
            .Migrate();

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Empty(db.Table<RunnerDataRow>().ToList());
    }

    [Fact]
    public void RepeatedCreateTableThenTableChanged_SameRun_Skips()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Version(2, m => m.CreateTable<RunnerDataRow>())
            .Version(3, m => m.TableChanged<RunnerDataRow>(b => b.Set(s => s.Value, 0)))
            .Migrate();

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(3, db.Pragmas.UserVersion);
    }

    [Fact]
    public void CreateTableThenDropModelColumn_SameRun_KeepsColumn()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Version(2, m => m.DropColumn<RunnerDataRow>("Value"))
            .Migrate();

        Assert.Contains(db.Schema.ListColumns<RunnerDataRow>(), c => c.Name == "Value");
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task CreateTableThenTableChangedAsync_SameRun_Skips()
    {
        using TestDatabase db = new();

        await db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Version(2, m => m.TableChanged<RunnerDataRow>(b => b.Set(s => s.Value, 0)))
            .MigrateAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(2, db.Pragmas.UserVersion);
        Assert.Empty(db.Table<RunnerDataRow>().ToList());
    }
}
