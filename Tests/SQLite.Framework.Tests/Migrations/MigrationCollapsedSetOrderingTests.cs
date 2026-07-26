using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CollapsedSetOrder")]
public class CollapsedSetOrderRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationCollapsedSetOrderingTests
{
    [Fact]
    public void SetReadingItsOwnColumnRunsAfterARunCallback()
    {
        Assert.Equal(StepwiseRows(RunCallbackChain), CollapsedRows(RunCallbackChain));
    }

    [Fact]
    public void SetReadingItsOwnColumnRunsAfterRawSql()
    {
        Assert.Equal(StepwiseRows(RawSqlChain), CollapsedRows(RawSqlChain));
    }

    [Fact]
    public void SetReadingItsOwnColumnRunsAfterAnInsert()
    {
        Assert.Equal(StepwiseRows(InsertChain), CollapsedRows(InsertChain));
    }

    [Fact]
    public void NewColumnIsReadableByALaterUpdateStep()
    {
        Assert.Equal(StepwiseRows(UpdateReadsNewColumnChain), CollapsedRows(UpdateReadsNewColumnChain));
    }

    [Fact]
    public void RunCallbackChainKeepsTheExpectedValues()
    {
        Assert.Equal([(11, "t2"), (6, "seeded")], StepwiseRows(RunCallbackChain));
    }

    [Fact]
    public void UpdateReadsNewColumnChainKeepsTheExpectedValues()
    {
        Assert.Equal([(11, "t3")], StepwiseRows(UpdateReadsNewColumnChain));
    }

    [Fact]
    public void ANewColumnFilledAfterAnEarlierDataStepKeepsItsValue()
    {
        Assert.Equal(StepwiseRows(NewColumnAfterDataChain), CollapsedRows(NewColumnAfterDataChain));
    }

    [Fact]
    public void NewColumnAfterDataChainKeepsTheExpectedValues()
    {
        Assert.Equal([(10, "filled"), (5, "filled")], StepwiseRows(NewColumnAfterDataChain));
    }

    private static void NewColumnAfterDataChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"CollapsedSetOrder\" (\"Id\", \"Val\") VALUES (5, 5)"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "filled")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("UPDATE \"CollapsedSetOrder\" SET \"Val\" = \"Val\""));
        }
    }

    [Fact]
    public void AConstantSetOnALiveColumnAfterAnEarlierDataStepKeepsItsValue()
    {
        Assert.Equal(StepwiseRows(ConstantOnLiveColumnChain), CollapsedRows(ConstantOnLiveColumnChain));
    }

    [Fact]
    public void ConstantOnLiveColumnChainKeepsTheExpectedValues()
    {
        Assert.Equal([(99, null), (99, null)], StepwiseRows(ConstantOnLiveColumnChain));
    }

    private static void ConstantOnLiveColumnChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"CollapsedSetOrder\" (\"Id\", \"Val\") VALUES (5, 5)"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Val, 99)));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("UPDATE \"CollapsedSetOrder\" SET \"Id\" = \"Id\""));
        }
    }

    private static void RunCallbackChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "t2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Run(ctx => ctx.Database.Execute(
                "INSERT INTO \"CollapsedSetOrder\" (\"Id\", \"Val\", \"Tag\") VALUES (2, 5, 'seeded')")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }
    }

    private static void RawSqlChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "t2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"CollapsedSetOrder\" SET \"Val\" = \"Val\" * 2"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }
    }

    private static void InsertChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "t2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Insert(new CollapsedSetOrderRow { Id = 3, Val = 7, Tag = "added" }));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }
    }

    private static void UpdateReadsNewColumnChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "t2")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Update<CollapsedSetOrderRow>(s => s.Set(x => x.Tag, "t3")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<CollapsedSetOrderRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }
    }

    private static List<(int Val, string? Tag)> StepwiseRows(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        for (int upTo = 2; upTo <= 4; upTo++)
        {
            SQLiteMigrationRunner runner = db.Schema.Migrations();
            chain(runner, upTo);
            runner.Migrate();
        }

        return Rows(db);
    }

    private static List<(int Val, string? Tag)> CollapsedRows(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        chain(runner, 4);
        runner.Migrate();
        return Rows(db);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"CollapsedSetOrder\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"CollapsedSetOrder\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Val, string? Tag)> Rows(TestDatabase db)
    {
        return db.Table<CollapsedSetOrderRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }
}
