using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigFMixedReads")]
public class MigFMixedReadRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }
}

[Table("MigFDroppedReads")]
public class MigFDroppedReadRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }
}

[Table("MigFEarlyFills")]
public class MigFEarlyFillRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationLegacyFillOrderingTests
{
    [Fact]
    public void AFillReadingLegacyAndModelColumnsSeesAnEarlierUpdateLikeStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedMixed(stepwise);
        MixedChain(stepwise.Schema.Migrations(), 2).Migrate();
        MixedChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedMixed(collapsed);
        MixedChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string? Name, string? Note)> stepwiseRows = MixedRows(stepwise);
        List<(string? Name, string? Note)> collapsedRows = MixedRows(collapsed);

        Assert.Equal([("n2", "keepme"), ("n2", "n2")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void AFillDeclaredAfterTheColumnDropIsSkippedLikeStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedDropped(stepwise);
        DroppedChain(stepwise.Schema.Migrations(), 2).Migrate();
        DroppedChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedDropped(collapsed);
        DroppedChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string? Name, string? Note)> stepwiseRows = DroppedRows(stepwise);
        List<(string? Name, string? Note)> collapsedRows = DroppedRows(collapsed);

        Assert.Equal([("a", null), ("b", null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static SQLiteMigrationRunner MixedChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<MigFMixedReadRow>(s => s.Set(x => x.Name, "n2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<MigFMixedReadRow>(
                s => s.Set(x => x.Note, x => SQLiteColumn.Of<string?>(x, "Legacy") ?? x.Name)));
        }

        return runner;
    }

    private static SQLiteMigrationRunner DroppedChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.DropColumn<MigFDroppedReadRow>("Legacy"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<MigFDroppedReadRow>(
                s => s.Set(x => x.Note, x => SQLiteColumn.Of<string?>(x, "Legacy"))));
        }

        return runner;
    }

    private static void SeedMixed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"MigFMixedReads\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"MigFMixedReads\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'a', 'keepme'), (2, 'b', NULL)");
        db.Pragmas.UserVersion = 1;
    }

    private static void SeedDropped(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"MigFDroppedReads\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"MigFDroppedReads\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'a', 'keepme'), (2, 'b', 'other')");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(string? Name, string? Note)> MixedRows(TestDatabase db)
    {
        return db.Table<MigFMixedReadRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Note })
            .ToList()
            .Select(x => (x.Name, x.Note))
            .ToList();
    }

    private static List<(string? Name, string? Note)> DroppedRows(TestDatabase db)
    {
        return db.Table<MigFDroppedReadRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Note })
            .ToList()
            .Select(x => (x.Name, x.Note))
            .ToList();
    }

    [Fact]
    public void AnEarlyOutsideFillRunsAtItsOwnVersionWhenALaterOwnColumnSetDefersTheRebuild()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedEarly(stepwise);
        EarlyChain(stepwise.Schema.Migrations(), 2).Migrate();
        EarlyChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedEarly(collapsed);
        EarlyChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(int Val, string? Tag)> stepwiseRows = EarlyRows(stepwise);
        List<(int Val, string? Tag)> collapsedRows = EarlyRows(collapsed);

        Assert.Equal([(11, "upd")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedEarly(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"MigFEarlyFills\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER NOT NULL, \"Tag\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"MigFEarlyFills\" (\"Id\", \"Val\", \"Tag\", \"Legacy\") VALUES (1, 10, NULL, 'lg')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner EarlyChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m
            .Update<MigFEarlyFillRow>(s => s.Set(x => x.Tag, "upd"))
            .TableChanged<MigFEarlyFillRow>(s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<MigFEarlyFillRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Tag)> EarlyRows(TestDatabase db)
    {
        return db.Table<MigFEarlyFillRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }
}
