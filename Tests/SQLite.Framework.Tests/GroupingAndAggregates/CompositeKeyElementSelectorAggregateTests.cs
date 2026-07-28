using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23kBasketRows")]
public class H23kBasketRow
{
    [Key]
    public int Id { get; set; }

    public int Region { get; set; }

    public int Channel { get; set; }

    public int UnitsSold { get; set; }
}

public class CompositeKeyElementSelectorAggregateTests
{
    [Fact]
    public void SumWithoutASelectorAggregatesTheSelectedElement()
    {
        using TestDatabase db = Setup(nameof(SumWithoutASelectorAggregatesTheSelectedElement));

        List<int> expected = Rows()
            .GroupBy(r => new { r.Region, r.Channel }, r => r.UnitsSold)
            .Select(g => g.Sum())
            .ToList();

        List<int> actual = db.Table<H23kBasketRow>()
            .GroupBy(r => new { r.Region, r.Channel }, r => r.UnitsSold)
            .Select(g => g.Sum())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumWithASelectorAggregatesTheSelectedElement()
    {
        using TestDatabase db = Setup(nameof(SumWithASelectorAggregatesTheSelectedElement));

        List<int> expected = Rows()
            .GroupBy(r => new { r.Region, r.Channel }, r => r.UnitsSold)
            .Select(g => g.Sum(v => v))
            .ToList();

        List<int> actual = db.Table<H23kBasketRow>()
            .GroupBy(r => new { r.Region, r.Channel }, r => r.UnitsSold)
            .Select(g => g.Sum(v => v))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowElementWithASimpleKeyAggregatesTheRowMember()
    {
        using TestDatabase db = Setup(nameof(RowElementWithASimpleKeyAggregatesTheRowMember));

        List<int> expected = Rows()
            .GroupBy(r => r.Region, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        List<int> actual = db.Table<H23kBasketRow>()
            .GroupBy(r => r.Region, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowElementWithARowKeyAggregatesTheRowMember()
    {
        using TestDatabase db = Setup(nameof(RowElementWithARowKeyAggregatesTheRowMember));

        List<int> expected = Rows()
            .GroupBy(r => r.Id, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        List<int> actual = db.Table<H23kBasketRow>()
            .GroupBy(r => r, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowElementWithACompositeKeyAggregatesTheRowMember()
    {
        using TestDatabase db = Setup(nameof(RowElementWithACompositeKeyAggregatesTheRowMember));

        List<int> expected = Rows()
            .GroupBy(r => new { r.Region, r.Channel }, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        List<int> actual = db.Table<H23kBasketRow>()
            .GroupBy(r => new { r.Region, r.Channel }, r => r)
            .Select(g => g.Sum(v => v.UnitsSold))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23kBasketRow> Rows()
    {
        return
        [
            new H23kBasketRow { Id = 1, Region = 1, Channel = 1, UnitsSold = 100 },
            new H23kBasketRow { Id = 2, Region = 1, Channel = 1, UnitsSold = 200 },
            new H23kBasketRow { Id = 3, Region = 2, Channel = 1, UnitsSold = 50 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23kBasketRow>().Schema.CreateTable();
        db.Table<H23kBasketRow>().AddRange(Rows());
        return db;
    }
}
