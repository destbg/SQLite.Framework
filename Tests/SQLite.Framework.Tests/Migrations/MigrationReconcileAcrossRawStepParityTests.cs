using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnATrans")]
public class ChnATransRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }
}

[Table("ChnAIdx")]
public class ChnAIdxRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

public class MigrationReconcileAcrossRawStepParityTests
{
    [Fact]
    public void AFillReadingAColumnAnEarlierRawStepAddsMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedTrans(stepwise);
        TransChain(stepwise.Schema.Migrations(), 2).Migrate();
        TransChain(stepwise.Schema.Migrations(), 3).Migrate();
        TransChain(stepwise.Schema.Migrations(), 4).Migrate();
        TransChain(stepwise.Schema.Migrations(), 5).Migrate();
        TransChain(stepwise.Schema.Migrations(), 6).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedTrans(collapsed);
        TransChain(collapsed.Schema.Migrations(), 6).Migrate();

        List<(int Id, string? Name, string? Note)> stepwiseRows = TransRows(stepwise);
        List<(int Id, string? Name, string? Note)> collapsedRows = TransRows(collapsed);
        List<string> stepwiseColumns = ColumnNames(stepwise, "ChnATrans");
        List<string> collapsedColumns = ColumnNames(collapsed, "ChnATrans");

        Assert.Equal([(1, "a", "x"), (2, "b", null)], stepwiseRows);
        Assert.Equal(["Id", "Name", "Note"], stepwiseColumns);
        Assert.Equal(stepwiseRows, collapsedRows);
        Assert.Equal(stepwiseColumns, collapsedColumns);
    }

    [Fact]
    public void AnIndexAnEarlierRawStepCreatesIsDroppedLikeStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedIdx(stepwise);
        IdxChain(stepwise.Schema.Migrations(), 2).Migrate();
        IdxChain(stepwise.Schema.Migrations(), 3).Migrate();
        IdxChain(stepwise.Schema.Migrations(), 4).Migrate();
        IdxChain(stepwise.Schema.Migrations(), 5).Migrate();
        IdxChain(stepwise.Schema.Migrations(), 6).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedIdx(collapsed);
        IdxChain(collapsed.Schema.Migrations(), 6).Migrate();

        List<(int Id, int Val)> stepwiseRows = IdxRows(stepwise);
        List<(int Id, int Val)> collapsedRows = IdxRows(collapsed);
        List<string> stepwiseIndexes = IndexNames(stepwise);
        List<string> collapsedIndexes = IndexNames(collapsed);

        Assert.Equal([(1, 10), (2, 20), (3, 30)], stepwiseRows);
        Assert.Empty(stepwiseIndexes);
        Assert.Equal(stepwiseRows, collapsedRows);
        Assert.Equal(stepwiseIndexes, collapsedIndexes);
    }

    private static void SeedTrans(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnATrans\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
        db.Execute("INSERT INTO \"ChnATrans\" (\"Id\", \"Name\") VALUES (1, 'a')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner TransChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("ALTER TABLE \"ChnATrans\" ADD COLUMN \"Extra\" TEXT"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"ChnATrans\" SET \"Extra\" = 'x'"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<ChnATransRow>(
                s => s.Set(x => x.Note, r => SQLiteColumn.Of<string?>(r, "Extra"))));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.Update<ChnATransRow>(x => x.Id == 999, s => s.Set(x => x.Name, "zzz")));
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.InsertIfMissing(x => x.Id, new ChnATransRow { Id = 2, Name = "b" }));
        }

        return runner;
    }

    private static List<(int Id, string? Name, string? Note)> TransRows(TestDatabase db)
    {
        return db.Table<ChnATransRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, x.Note })
            .ToList()
            .Select(x => (x.Id, x.Name, x.Note))
            .ToList();
    }

    private static void SeedIdx(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnAIdx\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"ChnAIdx\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner IdxChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("CREATE INDEX \"ChnAIdxRaw\" ON \"ChnAIdx\" (\"Val\")"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("INSERT INTO \"ChnAIdx\" (\"Id\", \"Val\") VALUES (2, 20)"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<ChnAIdxRow>());
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.Update<ChnAIdxRow>(x => x.Id == 999, s => s.Set(x => x.Val, 0)));
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.InsertIfMissing(x => x.Id, new ChnAIdxRow { Id = 3, Val = 30 }));
        }

        return runner;
    }

    private static List<(int Id, int Val)> IdxRows(TestDatabase db)
    {
        return db.Table<ChnAIdxRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Val })
            .ToList()
            .Select(x => (x.Id, x.Val))
            .ToList();
    }

    private static List<string> ColumnNames(TestDatabase db, string table)
    {
        return db.Pragmas.TableInfo(table).Select(c => c.Name).ToList();
    }

    private static List<string> IndexNames(TestDatabase db)
    {
        return db.Query<string>("SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = 'ChnAIdx' ORDER BY name");
    }
}
