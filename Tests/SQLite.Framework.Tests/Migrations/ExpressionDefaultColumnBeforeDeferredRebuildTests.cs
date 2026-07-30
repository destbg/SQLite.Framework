using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25fStampedTotals")]
public class H25fStampedTotal
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public string? Stamp { get; set; }
}

public class ExpressionDefaultColumnBeforeDeferredRebuildTests
{
    [Fact]
    public void ANewColumnWithAnExpressionDefaultDoesNotStopADeferredRebuildFill()
    {
        using ModelTestDatabase db = new(Model);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H25fStampedTotal>(s => s.Set(x => x.Amount, r => r.Amount + 1)))
            .Migrate();

        Assert.Equal([6L, 8L], Amounts(db));
    }

    private static void Model(SQLiteModelBuilder builder)
    {
        builder.Entity<H25fStampedTotal>()
            .Default(r => r.Stamp, () => SQLiteFunctions.SqliteVersion())
            .Check(r => r.Amount >= 0);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H25fStampedTotals\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H25fStampedTotals\" (\"Id\", \"Amount\") VALUES (1, 5), (2, 7)");
    }

    private static List<long> Amounts(TestDatabase db)
    {
        return db.Query<long>("SELECT \"Amount\" FROM \"H25fStampedTotals\" ORDER BY \"Id\"");
    }
}
