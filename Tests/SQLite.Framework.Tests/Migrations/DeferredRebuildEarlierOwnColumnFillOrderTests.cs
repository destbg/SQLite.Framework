using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26iLedgerEntries")]
public class H26iLedgerEntry
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    [SQLite.Framework.Attributes.Indexed(IsUnique = true)]
    public int Bonus { get; set; }
}

public class DeferredRebuildEarlierOwnColumnFillOrderTests
{
    [Fact]
    public void AFillReadingItsOwnColumnStaysAtItsVersionWhenALaterVersionDefersTheRebuild()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();
        Chain(stepwise.Schema.Migrations(), 4).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 4).Migrate();

        List<long> stepwiseAmounts = Amounts(stepwise);
        List<long> stepwiseBonuses = Bonuses(stepwise);

        Assert.Equal([110L, 114L], stepwiseAmounts);
        Assert.Equal([4L, 10L], stepwiseBonuses);
        Assert.Equal(stepwiseAmounts, Amounts(collapsed));
        Assert.Equal(stepwiseBonuses, Bonuses(collapsed));
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<H26iLedgerEntry>(s => s.Set(x => x.Amount, r => r.Amount * 2)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"H26iLedgerEntries\" SET \"Amount\" = \"Amount\" + 100"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<H26iLedgerEntry>(s => s.Set(x => x.Bonus, r => r.Bonus + 1)));
        }

        return runner;
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H26iLedgerEntries\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" INTEGER, \"Bonus\" INTEGER)");
        db.Execute("INSERT INTO \"H26iLedgerEntries\" (\"Id\", \"Amount\", \"Bonus\") VALUES (1, 5, 3), (2, 7, 9)");
    }

    private static List<long> Amounts(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Amount\" FROM \"H26iLedgerEntries\" ORDER BY \"Id\"");
    }

    private static List<long> Bonuses(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Bonus\" FROM \"H26iLedgerEntries\" ORDER BY \"Id\"");
    }
}
