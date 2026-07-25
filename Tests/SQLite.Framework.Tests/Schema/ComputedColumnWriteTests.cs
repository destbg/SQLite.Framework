using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedColumnWriteTests
{
    private static ModelTestDatabase CreateDb(bool stored = false)
    {
        ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity, stored: stored));
        db.Schema.CreateTable<ProductLine>();
        return db;
    }

    [Fact]
    public void Add_OmitsComputedColumn_RoundTrips()
    {
        using ModelTestDatabase db = CreateDb();

        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(5m * 3, row.Total);
    }

    [Fact]
    public void AddRange_OmitsComputedColumn_RoundTrips()
    {
        using ModelTestDatabase db = CreateDb();

        db.Table<ProductLine>().AddRange(new[]
        {
            new ProductLine { Id = 1, Price = 5m, Quantity = 3 },
            new ProductLine { Id = 2, Price = 2.5m, Quantity = 4 },
        });

        List<decimal> expected = new List<ProductLine>
        {
            new() { Id = 1, Price = 5m, Quantity = 3 },
            new() { Id = 2, Price = 2.5m, Quantity = 4 },
        }.Select(p => p.Price * p.Quantity).ToList();

        List<decimal> actual = db.Table<ProductLine>().OrderBy(p => p.Id).Select(p => p.Total).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Update_OmitsComputedColumn_Recomputes()
    {
        using ModelTestDatabase db = CreateDb();
        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        ProductLine row = db.Table<ProductLine>().Single();
        row.Quantity = 4;
        db.Table<ProductLine>().Update(row);

        ProductLine updated = db.Table<ProductLine>().Single();
        Assert.Equal(5m * 4, updated.Total);
    }

    [Fact]
    public void StoredComputed_Add_RoundTrips()
    {
        using ModelTestDatabase db = CreateDb(stored: true);

        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(5m * 3, row.Total);
    }

    [Fact]
    public void AddOrUpdate_OmitsComputedColumn_RoundTrips()
    {
        using ModelTestDatabase db = CreateDb();

        db.Table<ProductLine>().AddOrUpdate(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(5m * 3, row.Total);
    }

    [Fact]
    public void Upsert_Insert_OmitsComputedColumn_RoundTrips()
    {
        using ModelTestDatabase db = CreateDb();

        db.Table<ProductLine>().Upsert(
            new ProductLine { Id = 1, Price = 3m, Quantity = 4 },
            c => c.OnConflict(p => p.Id).DoUpdateAll());

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(3m * 4, row.Total);
    }

    [Fact]
    public void Upsert_ConflictUpdate_OmitsComputedColumn_Recomputes()
    {
        using ModelTestDatabase db = CreateDb();
        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        db.Table<ProductLine>().Upsert(
            new ProductLine { Id = 1, Price = 10m, Quantity = 2 },
            c => c.OnConflict(p => p.Id).DoUpdateAll());

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(10m * 2, row.Total);
    }
}
