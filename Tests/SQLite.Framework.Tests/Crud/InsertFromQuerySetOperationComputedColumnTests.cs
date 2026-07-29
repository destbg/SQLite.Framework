using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nSetOpComputedRows")]
public class H24nSetOpComputedRow
{
    [Key]
    public int Id { get; set; }

    public double Price { get; set; }

    public int Quantity { get; set; }

    public double Total { get; set; }
}

public class InsertFromQuerySetOperationComputedColumnTests
{
    [Fact]
    public void ConcatSourceExcludesTheComputedColumnFromEveryBranch()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24nSetOpComputedRow>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<H24nSetOpComputedRow>();
        db.Execute("INSERT INTO \"H24nSetOpComputedRows\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5.0, 3)");

        int inserted = db.Table<H24nSetOpComputedRow>().InsertFromQuery(
            db.Table<H24nSetOpComputedRow>().Where(p => p.Id < 0)
                .Concat(db.Table<H24nSetOpComputedRow>().Where(p => p.Id > 100)));

        Assert.Equal(0, inserted);
    }
}
