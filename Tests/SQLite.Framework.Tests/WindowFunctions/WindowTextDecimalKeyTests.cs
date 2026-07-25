using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PricedTicket")]
public class PricedTicketRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public decimal Amount { get; set; }
}

public class TicketWindowValues
{
    public int Id { get; set; }

    public long Rank { get; set; }

    public decimal Total { get; set; }
}

public class WindowTextDecimalKeyTests
{
    private static TestDatabase Seed(out List<PricedTicketRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<PricedTicketRow>().Schema.CreateTable();
        rows =
        [
            new PricedTicketRow { Id = 1, Price = 2.75m, Amount = 1m },
            new PricedTicketRow { Id = 2, Price = 9.5m, Amount = 2m },
            new PricedTicketRow { Id = 3, Price = 150m, Amount = 4m },
        ];
        db.Table<PricedTicketRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void WindowOrderByATextDecimalRanksNumerically()
    {
        using TestDatabase db = Seed(out List<PricedTicketRow> rows, nameof(WindowOrderByATextDecimalRanksNumerically));

        List<int> expected = rows.OrderBy(r => r.Price).Select(r => r.Id).ToList();
        List<int> actual = db.Table<PricedTicketRow>()
            .Select(o => new TicketWindowValues
            {
                Id = o.Id,
                Rank = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.Price),
            })
            .ToList().OrderBy(x => x.Rank).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowFilterOnATextDecimalComparesNumerically()
    {
        using TestDatabase db = Seed(out List<PricedTicketRow> rows, nameof(WindowFilterOnATextDecimalComparesNumerically));

        decimal expected = rows.Where(r => r.Price > 100m).Sum(r => r.Amount);
        decimal actual = db.Table<PricedTicketRow>()
            .Select(o => new TicketWindowValues
            {
                Id = o.Id,
                Total = SQLiteWindowFunctions.Sum(o.Amount)
                    .Filter(o.Price > 100m)
                    .Over(),
            })
            .First().Total;

        Assert.Equal(expected, actual);
    }
}
