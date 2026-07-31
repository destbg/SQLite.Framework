using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uQuoteRows")]
public class H26uQuoteRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }
}

[Table("H26uQuoteTagRows")]
public class H26uQuoteTagRow
{
    [Key]
    public int Id { get; set; }
}

public record H26uAmountBox(decimal Amount);

public class JoinConstructorDecimalTextProjectionTests
{
    [Fact]
    public void AJoinResultConstructorKeepsTheFullPrecisionOfATextStoredDecimal()
    {
        using TestDatabase db = Setup(nameof(AJoinResultConstructorKeepsTheFullPrecisionOfATextStoredDecimal));

        List<decimal> expected = Quotes()
            .Join(Tags(), q => q.Id, t => t.Id, (q, t) => new H26uAmountBox(q.Price))
            .Select(b => b.Amount)
            .OrderBy(v => v)
            .ToList();

        List<decimal> actual = db.Table<H26uQuoteRow>()
            .Join(db.Table<H26uQuoteTagRow>(), q => q.Id, t => t.Id, (q, t) => new H26uAmountBox(q.Price))
            .Select(b => b.Amount)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACrossJoinResultConstructorKeepsTheFullPrecisionOfATextStoredDecimal()
    {
        using TestDatabase db = Setup(nameof(ACrossJoinResultConstructorKeepsTheFullPrecisionOfATextStoredDecimal));

        List<H26uQuoteTagRow> tags = Tags();
        List<decimal> expected = Quotes()
            .SelectMany(_ => tags, (q, t) => new H26uAmountBox(q.Price))
            .Select(b => b.Amount)
            .OrderBy(v => v)
            .ToList();

        List<decimal> actual = db.Table<H26uQuoteRow>()
            .SelectMany(_ => db.Table<H26uQuoteTagRow>(), (q, t) => new H26uAmountBox(q.Price))
            .Select(b => b.Amount)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26uQuoteRow> Quotes()
    {
        return
        [
            new H26uQuoteRow { Id = 1, Price = 1234567890.1234567890m },
            new H26uQuoteRow { Id = 2, Price = 9876543210.9876543210m }
        ];
    }

    private static List<H26uQuoteTagRow> Tags()
    {
        return
        [
            new H26uQuoteTagRow { Id = 1 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.DecimalStorage = DecimalStorageMode.Text, methodName);
        db.Table<H26uQuoteRow>().Schema.CreateTable();
        db.Table<H26uQuoteTagRow>().Schema.CreateTable();
        db.Table<H26uQuoteRow>().AddRange(Quotes());
        db.Table<H26uQuoteTagRow>().AddRange(Tags());
        return db;
    }
}
