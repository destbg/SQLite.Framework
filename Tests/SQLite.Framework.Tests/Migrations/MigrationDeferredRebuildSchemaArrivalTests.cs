using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21aArrivalTag")]
public class H21aArrivalTagRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

[Table("H21aArrivalLegacy")]
public class H21aArrivalLegacyRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationDeferredRebuildSchemaArrivalTests
{
    [Fact]
    public void NewColumnFillRunsAfterTheColumnExists()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedTag(stepwise);
        TagChain(stepwise.Schema.Migrations(), 2).Migrate();
        TagChain(stepwise.Schema.Migrations(), 3).Migrate();
        TagChain(stepwise.Schema.Migrations(), 4).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedTag(collapsed);
        TagChain(collapsed.Schema.Migrations(), 4).Migrate();

        List<(int Val, string? Tag)> stepwiseRows = TagRows(stepwise);
        List<(int Val, string? Tag)> collapsedRows = TagRows(collapsed);

        Assert.Equal([(11, "t2")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void DataStepSeesTheColumnAddedByAnEarlierVersion()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedLegacy(stepwise);
        LegacyChain(stepwise.Schema.Migrations(), 2).Migrate();
        LegacyChain(stepwise.Schema.Migrations(), 3).Migrate();
        LegacyChain(stepwise.Schema.Migrations(), 4).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedLegacy(collapsed);
        LegacyChain(collapsed.Schema.Migrations(), 4).Migrate();

        List<(int Val, string? Tag)> stepwiseRows = LegacyRows(stepwise);
        List<(int Val, string? Tag)> collapsedRows = LegacyRows(collapsed);

        Assert.Equal([(11, "t3")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedTag(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H21aArrivalTag\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"H21aArrivalTag\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner TagChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<H21aArrivalTagRow>(s => s.Set(x => x.Tag, "t2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Delete<H21aArrivalTagRow>(x => x.Id == 999));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<H21aArrivalTagRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Tag)> TagRows(TestDatabase db)
    {
        return db.Table<H21aArrivalTagRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }

    private static void SeedLegacy(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H21aArrivalLegacy\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H21aArrivalLegacy\" (\"Id\", \"Val\", \"Legacy\") VALUES (1, 10, 'keepme')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner LegacyChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<H21aArrivalLegacyRow>(
            s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Update<H21aArrivalLegacyRow>(s => s.Set(x => x.Tag, "t3")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<H21aArrivalLegacyRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Tag)> LegacyRows(TestDatabase db)
    {
        return db.Table<H21aArrivalLegacyRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }
}
