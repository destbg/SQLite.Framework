using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26iStampedTotals")]
public class H26iStampedTotal
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public string? Stamp { get; set; }
}

public class ExpressionDefaultColumnFillBeforeDeferredRebuildTests
{
    [Fact]
    public void AnEarlierFillOnANewExpressionDefaultColumnRunsWhenALaterVersionDefersTheRebuild()
    {
        using ModelTestDatabase stepwise = new(Model);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 1).Migrate();
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();

        using ModelTestDatabase collapsed = new(Model);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 3).Migrate();

        List<long> stepwiseAmounts = Amounts(stepwise);
        List<string> stepwiseStamps = Stamps(stepwise);

        Assert.Equal([6L, 8L], stepwiseAmounts);
        Assert.Equal(["kept", "kept"], stepwiseStamps);
        Assert.Equal(stepwiseAmounts, Amounts(collapsed));
        Assert.Equal(stepwiseStamps, Stamps(collapsed));
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m.Sql("SELECT 1"));
        if (upTo >= 2)
        {
            runner.Version(2, m => m.TableChanged<H26iStampedTotal>(s => s.Set(x => x.Stamp, "kept")));
        }

        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H26iStampedTotal>(s => s.Set(x => x.Amount, r => r.Amount + 1)));
        }

        return runner;
    }

    private static void Model(SQLiteModelBuilder builder)
    {
        builder.Entity<H26iStampedTotal>()
            .Default(r => r.Stamp, () => SQLiteFunctions.SqliteVersion())
            .Check(r => r.Amount >= 0);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H26iStampedTotals\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H26iStampedTotals\" (\"Id\", \"Amount\") VALUES (1, 5), (2, 7)");
    }

    private static List<long> Amounts(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Amount\" FROM \"H26iStampedTotals\" ORDER BY \"Id\"");
    }

    private static List<string> Stamps(TestDatabase db)
    {
        return db.Query<string>("SELECT \"Stamp\" FROM \"H26iStampedTotals\" ORDER BY \"Id\"");
    }
}
