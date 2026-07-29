using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nExecuteUpdateComputedRows")]
public class H24nExecuteUpdateComputedRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }
}

public class ExecuteUpdateComputedColumnTests
{
    [Fact]
    public void ExecuteUpdateNamingTheComputedColumnStillUpdatesTheOtherColumn()
    {
        using ModelTestDatabase db = Setup(nameof(ExecuteUpdateNamingTheComputedColumnStillUpdatesTheOtherColumn));

        db.Table<H24nExecuteUpdateComputedRow>().ExecuteUpdate(s => s
            .Set(p => p.Quantity, 4)
            .Set(p => p.Total, 0m));

        H24nExecuteUpdateComputedRow row = db.Table<H24nExecuteUpdateComputedRow>().Single();

        Assert.Equal(4, row.Quantity);
        Assert.Equal(5m * 4, row.Total);
    }

    [Fact]
    public void ExecuteUpdateNamingOnlyTheComputedColumnLeavesTheRowUnchanged()
    {
        using ModelTestDatabase db = Setup(nameof(ExecuteUpdateNamingOnlyTheComputedColumnLeavesTheRowUnchanged));

        db.Table<H24nExecuteUpdateComputedRow>().ExecuteUpdate(s => s.Set(p => p.Total, 99m));

        H24nExecuteUpdateComputedRow row = db.Table<H24nExecuteUpdateComputedRow>().Single();

        Assert.Equal(3, row.Quantity);
        Assert.Equal(5m * 3, row.Total);
    }

    [Fact]
    public void ExecuteUpdateWithAnExpressionSetterOnTheComputedColumnLeavesTheRowUnchanged()
    {
        using ModelTestDatabase db = Setup(nameof(ExecuteUpdateWithAnExpressionSetterOnTheComputedColumnLeavesTheRowUnchanged));

        int updated = db.Table<H24nExecuteUpdateComputedRow>().ExecuteUpdate(s => s.Set(p => p.Total, p => p.Price + 1m));

        H24nExecuteUpdateComputedRow row = db.Table<H24nExecuteUpdateComputedRow>().Single();

        Assert.Equal(0, updated);
        Assert.Equal(5m * 3, row.Total);
    }

    private static ModelTestDatabase Setup(string methodName)
    {
        ModelTestDatabase db = new(
            model => model.Entity<H24nExecuteUpdateComputedRow>()
                .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true),
            methodName);
        db.Schema.CreateTable<H24nExecuteUpdateComputedRow>();
        db.Table<H24nExecuteUpdateComputedRow>().Add(new H24nExecuteUpdateComputedRow { Id = 1, Price = 5m, Quantity = 3 });
        return db;
    }
}
