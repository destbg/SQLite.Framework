using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25iComputedTotalRows")]
public class H25iComputedTotalRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }
}

public class ReturningExecuteUpdateSkippedSetterTests
{
    [Fact]
    public void ReturningExecuteUpdateNamingOnlyTheComputedColumnWritesNothingAndReturnsNoRows()
    {
        using ModelTestDatabase db = Setup(nameof(ReturningExecuteUpdateNamingOnlyTheComputedColumnWritesNothingAndReturnsNoRows));

        List<decimal> written = db.Table<H25iComputedTotalRow>()
            .Where(r => r.Id > 0)
            .Returning(r => r.Total)
            .ExecuteUpdate(s => s.Set(r => r.Total, 99m));

        Assert.Empty(written);
        Assert.Equal(3, db.ExecuteScalar<int>("SELECT \"Quantity\" FROM \"H25iComputedTotalRows\" WHERE \"Id\" = 1"));
        Assert.Equal(5m * 3, db.Table<H25iComputedTotalRow>().Single().Total);
    }

    [Fact]
    public void ReturningExecuteUpdateNamingTheComputedColumnBesideAnotherOneStillWritesTheOtherOne()
    {
        using ModelTestDatabase db = Setup(nameof(ReturningExecuteUpdateNamingTheComputedColumnBesideAnotherOneStillWritesTheOtherOne));

        List<decimal> written = db.Table<H25iComputedTotalRow>()
            .Where(r => r.Id > 0)
            .Returning(r => r.Total)
            .ExecuteUpdate(s => s
                .Set(r => r.Total, 99m)
                .Set(r => r.Quantity, 4));

        Assert.Single(written);
        Assert.Equal(5m * 4, db.Table<H25iComputedTotalRow>().Single().Total);
    }

    private static ModelTestDatabase Setup(string methodName)
    {
        ModelTestDatabase db = new(
            model => model.Entity<H25iComputedTotalRow>()
                .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true),
            methodName);
        db.Schema.CreateTable<H25iComputedTotalRow>();
        db.Table<H25iComputedTotalRow>().Add(new H25iComputedTotalRow { Id = 1, Price = 5m, Quantity = 3 });
        return db;
    }
}
