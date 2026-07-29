using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24dCheckedTotals")]
public class H24dCheckedTotal
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class CheckConstraintFillOnRequiredColumnTests
{
    [Fact]
    public void AFillThatMakesRowsSatisfyANewCheckConstraintRunsOnAnAlreadyRequiredColumn()
    {
        using ModelTestDatabase db = new(Model);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<H24dCheckedTotal>(s => s.Set(x => x.Amount, r => r.Amount * 100)))
            .Migrate();

        Assert.Equal([500L, 700L], Amounts(db));
    }

    [Fact]
    public void AFillThatMakesRowsSatisfyANewCheckConstraintRunsOnAnAlreadyRequiredColumnAfterARawStep()
    {
        using ModelTestDatabase db = new(Model);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H24dCheckedTotal>(s => s.Set(x => x.Amount, r => r.Amount * 100)))
            .Migrate();

        Assert.Equal([500L, 700L], Amounts(db));
    }

    private static void Model(SQLiteModelBuilder builder)
    {
        builder.Entity<H24dCheckedTotal>().Check(r => r.Amount >= 100);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H24dCheckedTotals\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H24dCheckedTotals\" (\"Id\", \"Amount\") VALUES (1, 5), (2, 7)");
    }

    private static List<long> Amounts(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Amount\" FROM \"H24dCheckedTotals\" ORDER BY \"Id\"");
    }
}
