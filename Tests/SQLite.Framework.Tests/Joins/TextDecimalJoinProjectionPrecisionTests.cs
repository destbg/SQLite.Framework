using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26rPrecisePriceRows")]
public class H26rPrecisePriceRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }
}

[Table("H26rPriceTagRows")]
public class H26rPriceTagRow
{
    [Key]
    public int Id { get; set; }

    public int PriceId { get; set; }

    public string Tag { get; set; } = string.Empty;
}

public class TextDecimalJoinProjectionPrecisionTests
{
    [Fact]
    public void APlainProjectionKeepsTheStoredPrecisionOfATextDecimal()
    {
        using TestDatabase db = Setup();

        List<decimal> expected = Prices().OrderBy(p => p.Id).Select(p => p.Price).ToList();

        List<decimal> actual = db.Table<H26rPrecisePriceRow>()
            .Select(p => new { p.Id, p.Price })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Price)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AJoinResultSelectorKeepsTheStoredPrecisionOfATextDecimal()
    {
        using TestDatabase db = Setup();

        List<decimal> expected = Prices()
            .Join(Tags(), p => p.Id, t => t.PriceId, (p, t) => new { p.Price, t.Tag })
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Price)
            .ToList();

        List<decimal> actual = db.Table<H26rPrecisePriceRow>()
            .Join(db.Table<H26rPriceTagRow>(), p => p.Id, t => t.PriceId, (p, t) => new { p.Price, t.Tag })
            .AsEnumerable()
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Price)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ASecondFromSourceResultSelectorKeepsTheStoredPrecisionOfATextDecimal()
    {
        using TestDatabase db = Setup();

        List<decimal> expected = Prices()
            .SelectMany(_ => Tags(), (p, t) => new { p.Price, t.Tag })
            .OrderBy(x => x.Price)
            .ThenBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Price)
            .ToList();

        List<decimal> actual = db.Table<H26rPrecisePriceRow>()
            .SelectMany(_ => db.Table<H26rPriceTagRow>(), (p, t) => new { p.Price, t.Tag })
            .AsEnumerable()
            .OrderBy(x => x.Price)
            .ThenBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Price)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26rPrecisePriceRow> Prices()
    {
        return
        [
            new H26rPrecisePriceRow { Id = 1, Price = 1.234567890123456789m },
            new H26rPrecisePriceRow { Id = 2, Price = 9.876543210987654321m }
        ];
    }

    private static List<H26rPriceTagRow> Tags()
    {
        return
        [
            new H26rPriceTagRow { Id = 1, PriceId = 1, Tag = "a" },
            new H26rPriceTagRow { Id = 2, PriceId = 2, Tag = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H26rPrecisePriceRow>().Schema.CreateTable();
        db.Table<H26rPriceTagRow>().Schema.CreateTable();
        db.Table<H26rPrecisePriceRow>().AddRange(Prices());
        db.Table<H26rPriceTagRow>().AddRange(Tags());
        return db;
    }
}
