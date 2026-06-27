using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigCarry")]
public class MigCarryRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Copy { get; set; }
}

[Table("MigSeed")]
public class MigSeedRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class MigrationRunnerBehaviorTests
{
    private static string? TriggerSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{name}'");

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Set_ReadsColumnBeingRemoved_CopiesValue(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigCarry\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"MigCarry\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'a', 'carried')");

        db.Table<MigCarryRow>().Schema.Migrate(mode, m => m.Set(x => x.Copy, x => SQLiteColumn.Of<string>(x, "Legacy")));

        MigCarryRow row = db.Table<MigCarryRow>().Single();
        Assert.Equal("carried", row.Copy);
        Assert.DoesNotContain(db.Schema.ListColumns<MigCarryRow>(), c => c.Name == "Legacy");
    }

    [Fact]
    public void FreshDatabase_RawSqlSchemaStep_Runs()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"PlainSql\" (\"Id\" INTEGER PRIMARY KEY, \"V\" INTEGER NOT NULL)"))
            .Migrate();

        Assert.True(db.Schema.TableExists("PlainSql"));
    }

    [Fact]
    public void FreshDatabase_TableChangedThenSeedSql_RunsSeed()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigSeedRow>()
                .Sql("INSERT INTO \"MigSeed\" (\"Id\", \"Value\") VALUES (1, 100)"))
            .Migrate();

        Assert.Equal(100, db.Table<MigSeedRow>().Single().Value);
    }

    [Fact]
    public void FreshDatabase_DropSteps_AreSkipped()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigSeedRow>()
                .DropColumn<MigSeedRow>("Value")
                .DropTable("AbsentLegacy"))
            .Migrate();

        Assert.True(db.Table<MigSeedRow>().Schema.ColumnExists("Value"));
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void DropTable_ExternalContentFts_RemovesSyncTriggers()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();
        Assert.NotNull(TriggerSql(db, "ArticleSearch_sync_ai"));

        db.Schema.Migrations()
            .Version(1, m => m.DropTable<ArticleSearch>())
            .Migrate();

        db.Table<Article>().Add(new Article { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow });
        Assert.Single(db.Table<Article>().ToList());
    }

    [Fact]
    public void DropColumnExplicit_AfterReconcileRemovesSameColumn_Succeeds()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Gone\" INTEGER)");
        db.Execute("INSERT INTO \"RunDrop\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 5, 9)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDropRow>().DropColumn<RunnerDropRow>("Gone"))
            .Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<RunnerDropRow>(), c => c.Name == "Gone");
        Assert.Equal(5, db.Table<RunnerDropRow>().Single().Keep);
    }
}
