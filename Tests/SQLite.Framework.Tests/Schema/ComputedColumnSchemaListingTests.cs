using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23pComputedListingRows")]
public class H23pComputedListingRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal Total { get; set; }
}

public class ComputedColumnSchemaListingTests
{
    [Fact]
    public void ColumnExistsReportsTheComputedColumn()
    {
        using ModelTestDatabase db = Setup(nameof(ColumnExistsReportsTheComputedColumn));

        List<string> liveColumns = LiveColumnNames(db);

        Assert.Contains("Total", liveColumns);
        Assert.True(db.Schema.ColumnExists<H23pComputedListingRow>("Total"));
    }

    [Fact]
    public void ListColumnsReturnsEveryLiveColumn()
    {
        using ModelTestDatabase db = Setup(nameof(ListColumnsReturnsEveryLiveColumn));

        List<string> expected = LiveColumnNames(db);
        List<string> actual = db.Schema.ListColumns<H23pComputedListingRow>().Select(c => c.Name).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<string> LiveColumnNames(ModelTestDatabase db)
    {
        return db.Query<Dictionary<string, object?>>("PRAGMA table_xinfo('H23pComputedListingRows')")
            .Select(row => (string)row["name"]!)
            .ToList();
    }

    private static ModelTestDatabase Setup(string methodName)
    {
        ModelTestDatabase db = new(
            model => model.Entity<H23pComputedListingRow>()
                .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true),
            methodName);
        db.Schema.CreateTable<H23pComputedListingRow>();
        db.Table<H23pComputedListingRow>().Add(new H23pComputedListingRow { Id = 1, Price = 5m, Quantity = 3 });
        return db;
    }
}
