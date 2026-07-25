using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mTicketRows")]
public class H21mTicketRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextScalarOrderingParityTests
{
    [Fact]
    public void OrderByProjectedValueSortsNumerically()
    {
        using TestDatabase db = Seed();
        List<H21mTicketRow> local = Rows();

        List<decimal> expected = local.Select(r => r.Amount).OrderBy(x => x).ToList();

        List<decimal> actual = db.Table<H21mTicketRow>().Select(r => r.Amount).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingProjectedValueSortsNumerically()
    {
        using TestDatabase db = Seed();
        List<H21mTicketRow> local = Rows();

        List<decimal> expected = local.Select(r => r.Amount).OrderByDescending(x => x).ToList();

        List<decimal> actual = db.Table<H21mTicketRow>().Select(r => r.Amount).OrderByDescending(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByColumnBeforeProjectionSortsNumerically()
    {
        using TestDatabase db = Seed();
        List<H21mTicketRow> local = Rows();

        List<decimal> expected = local.OrderBy(r => r.Amount).Select(r => r.Amount).ToList();

        List<decimal> actual = db.Table<H21mTicketRow>().OrderBy(r => r.Amount).Select(r => r.Amount).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21mTicketRow> Rows()
    {
        return
        [
            new H21mTicketRow { Id = 1, Amount = 9.99m },
            new H21mTicketRow { Id = 2, Amount = 10.11m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H21mTicketRow>().Schema.CreateTable();
        db.Table<H21mTicketRow>().AddRange(Rows());
        return db;
    }
}
