using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReconciledLedger")]
public class ReconciledLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }
}

public class MigrationTableChangedCreatorFillTests
{
    [Fact]
    public void FreshRunAppliesALaterFillWhenTableChangedCreatesTheTable()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.TableChanged<ReconciledLedgerRow>().Insert(new ReconciledLedgerRow { Id = 1, Price = 10 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.TableChanged<ReconciledLedgerRow>().Insert(new ReconciledLedgerRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<ReconciledLedgerRow>(s => s.Set(x => x.Price, 99)))
            .Migrate();

        using TestDatabase fresh = new(useFile: true);
        fresh.Schema.Migrations()
            .Version(1, m => m.TableChanged<ReconciledLedgerRow>().Insert(new ReconciledLedgerRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<ReconciledLedgerRow>(s => s.Set(x => x.Price, 99)))
            .Migrate();

        Assert.Equal(
            stepwise.Table<ReconciledLedgerRow>().Single().Price,
            fresh.Table<ReconciledLedgerRow>().Single().Price);
    }
}
