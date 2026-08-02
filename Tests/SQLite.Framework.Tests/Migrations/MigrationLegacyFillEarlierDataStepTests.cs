using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigBFillOrder")]
public class MigBFillOrderRow
{
    [Key]
    public int Id { get; set; }

    public string? Code { get; set; }
}

[Table("MigBFillLog")]
public class MigBFillLogRow
{
    [Key]
    public int Id { get; set; }

    public string? Code { get; set; }
}

public class MigrationLegacyFillEarlierDataStepTests
{
    [Fact]
    public void AnEarlierUpdateDoesNotOverwriteALaterLegacyFill()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedOrder(stepwise);
        SQLiteMigrationRunner stepwiseRun1 = stepwise.Schema.Migrations();
        UpdateThenFillChain(stepwiseRun1, 1);
        stepwiseRun1.Migrate();
        SQLiteMigrationRunner stepwiseRun2 = stepwise.Schema.Migrations();
        UpdateThenFillChain(stepwiseRun2, 2);
        stepwiseRun2.Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedOrder(collapsed);
        SQLiteMigrationRunner collapsedRun = collapsed.Schema.Migrations();
        UpdateThenFillChain(collapsedRun, 2);
        collapsedRun.Migrate();

        string? stepwiseCode = OrderCode(stepwise);
        string? collapsedCode = OrderCode(collapsed);

        Assert.Equal("recovered", stepwiseCode);
        Assert.Equal(stepwiseCode, collapsedCode);
    }

    [Fact]
    public void AnEarlierRawStepReadsTheColumnBeforeALaterLegacyFill()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedOrder(stepwise);
        SQLiteMigrationRunner stepwiseRun1 = stepwise.Schema.Migrations();
        LogThenFillChain(stepwiseRun1, 1);
        stepwiseRun1.Migrate();
        SQLiteMigrationRunner stepwiseRun2 = stepwise.Schema.Migrations();
        LogThenFillChain(stepwiseRun2, 2);
        stepwiseRun2.Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedOrder(collapsed);
        SQLiteMigrationRunner collapsedRun = collapsed.Schema.Migrations();
        LogThenFillChain(collapsedRun, 2);
        collapsedRun.Migrate();

        string? stepwiseLogged = LoggedCode(stepwise);
        string? collapsedLogged = LoggedCode(collapsed);

        Assert.Equal("original", stepwiseLogged);
        Assert.Equal("recovered", OrderCode(stepwise));
        Assert.Equal(OrderCode(stepwise), OrderCode(collapsed));
        Assert.Equal(stepwiseLogged, collapsedLogged);
    }

    private static void UpdateThenFillChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m.Update<MigBFillOrderRow>(s => s.Set(x => x.Code, "written")));
        if (upTo >= 2)
        {
            runner.Version(2, m => m.TableChanged<MigBFillOrderRow>(
                s => s.Set(x => x.Code, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        }
    }

    private static void LogThenFillChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m
            .CreateTable<MigBFillLogRow>()
            .Sql("INSERT INTO \"MigBFillLog\" (\"Id\", \"Code\") SELECT \"Id\", \"Code\" FROM \"MigBFillOrder\""));
        if (upTo >= 2)
        {
            runner.Version(2, m => m.TableChanged<MigBFillOrderRow>(
                s => s.Set(x => x.Code, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        }
    }

    private static void SeedOrder(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"MigBFillOrder\" (\"Id\" INTEGER PRIMARY KEY, \"Code\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"MigBFillOrder\" (\"Id\", \"Code\", \"Legacy\") VALUES (1, 'original', 'recovered')");
    }

    private static string? OrderCode(TestDatabase db)
    {
        return db.Table<MigBFillOrderRow>().Single().Code;
    }

    private static string? LoggedCode(TestDatabase db)
    {
        return db.Table<MigBFillLogRow>().Single().Code;
    }
}
