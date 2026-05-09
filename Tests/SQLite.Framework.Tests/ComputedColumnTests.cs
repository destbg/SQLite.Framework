using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedColumnTests
{
    [Fact]
    public void Computed_Virtual_ProducesValueOnRead()
    {
        using TestDatabase db = new();
        db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity)
            .CreateTable();

        db.Execute(
            "INSERT INTO ProductLines (Id, Price, Quantity) VALUES (1, 5.0, 3), (2, 2.5, 4)");

        List<ProductLine> rows = db.Table<ProductLine>().OrderBy(p => p.Id).ToList();
        Assert.Equal(15.0m, rows[0].Total);
        Assert.Equal(10.0m, rows[1].Total);
    }

    [Fact]
    public void Computed_Stored_ProducesValueOnRead()
    {
        using TestDatabase db = new();
        db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true)
            .CreateTable();

        db.Execute(
            "INSERT INTO ProductLines (Id, Price, Quantity) VALUES (1, 5.0, 3)");

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(15.0m, row.Total);
    }

    [Fact]
    public void Computed_DoesNotAcceptDirectWrites()
    {
        using TestDatabase db = new();
        db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity)
            .CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO ProductLines (Id, Price, Quantity, Total) VALUES (1, 1, 1, 99)"));
    }

    [Fact]
    public void Computed_NotAPropertyExpression_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<ProductLine>()
                .Computed(p => p.Price + 1, p => p.Price));
    }
}
