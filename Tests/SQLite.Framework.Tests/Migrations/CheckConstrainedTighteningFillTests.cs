using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23dCheckedAmounts")]
public class H23dCheckedAmount
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class CheckConstrainedTighteningFillTests
{
    [Fact]
    public void AFillThatMakesRowsSatisfyACheckConstraintRunsWithAnEarlierRawStepToo()
    {
        using ModelTestDatabase stepwise = new(Model);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Migrate();
        Exception? stepwiseError = Record.Exception(() => stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H23dCheckedAmount>(s => s.Set(x => x.Amount, r => r.Amount * 100)))
            .Migrate());

        using ModelTestDatabase collapsed = new(Model);
        Seed(collapsed);
        Exception? collapsedError = Record.Exception(() => collapsed.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H23dCheckedAmount>(s => s.Set(x => x.Amount, r => r.Amount * 100)))
            .Migrate());

        Assert.Null(stepwiseError);
        Assert.Equal(stepwiseError?.GetType(), collapsedError?.GetType());
        Assert.Equal([500L, 700L], Amounts(stepwise));
        Assert.Equal(Amounts(stepwise), Amounts(collapsed));
    }

    private static void Model(SQLiteModelBuilder builder)
    {
        builder.Entity<H23dCheckedAmount>().Check(r => r.Amount >= 100);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H23dCheckedAmounts\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" INTEGER)");
        db.Execute("INSERT INTO \"H23dCheckedAmounts\" (\"Id\", \"Amount\") VALUES (1, 5), (2, 7)");
    }

    private static List<long> Amounts(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Amount\" FROM \"H23dCheckedAmounts\" ORDER BY \"Id\"");
    }
}
