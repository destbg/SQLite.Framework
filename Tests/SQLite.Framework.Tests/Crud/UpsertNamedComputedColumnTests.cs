using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23pUpsertComputedRows")]
public class H23pUpsertComputedRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }
}

public class UpsertNamedComputedColumnTests
{
    [Fact]
    public void DoUpdateAllRecomputesTheComputedColumnOnConflict()
    {
        using ModelTestDatabase db = Setup(nameof(DoUpdateAllRecomputesTheComputedColumnOnConflict));

        db.Table<H23pUpsertComputedRow>().Upsert(
            new H23pUpsertComputedRow { Id = 1, Price = 5m, Quantity = 4 },
            c => c.OnConflict(p => p.Id).DoUpdateAll());

        H23pUpsertComputedRow row = db.Table<H23pUpsertComputedRow>().Single();

        Assert.Equal(4, row.Quantity);
        Assert.Equal(5m * 4, row.Total);
    }

    [Fact]
    public void DoUpdateNamingTheComputedColumnRecomputesItOnConflict()
    {
        using ModelTestDatabase db = Setup(nameof(DoUpdateNamingTheComputedColumnRecomputesItOnConflict));

        db.Table<H23pUpsertComputedRow>().Upsert(
            new H23pUpsertComputedRow { Id = 1, Price = 5m, Quantity = 4 },
            c => c.OnConflict(p => p.Id).DoUpdate(p => p.Quantity, p => p.Total));

        H23pUpsertComputedRow row = db.Table<H23pUpsertComputedRow>().Single();

        Assert.Equal(4, row.Quantity);
        Assert.Equal(5m * 4, row.Total);
    }

    [Fact]
    public void DoUpdateSetterNamingTheComputedColumnRecomputesItOnConflict()
    {
        using ModelTestDatabase db = Setup(nameof(DoUpdateSetterNamingTheComputedColumnRecomputesItOnConflict));

        db.Table<H23pUpsertComputedRow>().Upsert(
            new H23pUpsertComputedRow { Id = 1, Price = 5m, Quantity = 4 },
            c => c.OnConflict(p => p.Id).DoUpdate(s => s
                .Set(p => p.Quantity, (current, excluded) => excluded.Quantity)
                .Set(p => p.Total, (current, excluded) => excluded.Total)));

        H23pUpsertComputedRow row = db.Table<H23pUpsertComputedRow>().Single();

        Assert.Equal(4, row.Quantity);
        Assert.Equal(5m * 4, row.Total);
    }

    [Fact]
    public void DoUpdateSetterTargetingOnlyTheComputedColumnLeavesTheRowUnchanged()
    {
        using ModelTestDatabase db = Setup(nameof(DoUpdateSetterTargetingOnlyTheComputedColumnLeavesTheRowUnchanged));

        db.Table<H23pUpsertComputedRow>().Upsert(
            new H23pUpsertComputedRow { Id = 1, Price = 9m, Quantity = 8 },
            c => c.OnConflict(p => p.Id).DoUpdate(s => s
                .Set(p => p.Total, (current, excluded) => excluded.Total)));

        H23pUpsertComputedRow row = db.Table<H23pUpsertComputedRow>().Single();

        Assert.Equal(3, row.Quantity);
        Assert.Equal(5m * 3, row.Total);
    }

    [Fact]
    public void DoUpdateNamingOnlyTheComputedColumnLeavesTheRowUnchanged()
    {
        using ModelTestDatabase db = Setup(nameof(DoUpdateNamingOnlyTheComputedColumnLeavesTheRowUnchanged));

        db.Table<H23pUpsertComputedRow>().Upsert(
            new H23pUpsertComputedRow { Id = 1, Price = 9m, Quantity = 8 },
            c => c.OnConflict(p => p.Id).DoUpdate(p => p.Total));

        H23pUpsertComputedRow row = db.Table<H23pUpsertComputedRow>().Single();

        Assert.Equal(3, row.Quantity);
        Assert.Equal(5m * 3, row.Total);
    }

    private static ModelTestDatabase Setup(string methodName)
    {
        ModelTestDatabase db = new(
            model => model.Entity<H23pUpsertComputedRow>()
                .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true),
            methodName);
        db.Schema.CreateTable<H23pUpsertComputedRow>();
        db.Table<H23pUpsertComputedRow>().Add(new H23pUpsertComputedRow { Id = 1, Price = 5m, Quantity = 3 });
        return db;
    }
}
