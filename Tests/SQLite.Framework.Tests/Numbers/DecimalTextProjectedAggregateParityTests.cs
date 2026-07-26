using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22gLedgerRows")]
public class H22gLedgerRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextProjectedAggregateParityTests
{
    [Fact]
    public void MinOverAProjectedDecimalColumnMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H22gLedgerRow> local = Rows();

        decimal expected = local.Select(r => r.Amount).Min();

        decimal actual = db.Table<H22gLedgerRow>().Select(r => r.Amount).Min();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverAProjectedDecimalColumnMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H22gLedgerRow> local = Rows();

        decimal expected = local.Select(r => r.Amount).Max();

        decimal actual = db.Table<H22gLedgerRow>().Select(r => r.Amount).Max();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverAFilteredProjectedDecimalColumnMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H22gLedgerRow> local = Rows();

        decimal expected = local.Where(r => r.Id != 3).Select(r => r.Amount).Min();

        decimal actual = db.Table<H22gLedgerRow>().Where(r => r.Id != 3).Select(r => r.Amount).Min();

        Assert.Equal(expected, actual);
    }

    private static List<H22gLedgerRow> Rows()
    {
        return
        [
            new H22gLedgerRow { Id = 1, Amount = 9.99m },
            new H22gLedgerRow { Id = 2, Amount = 10.11m },
            new H22gLedgerRow { Id = 3, Amount = 100.5m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H22gLedgerRow>().Schema.CreateTable();
        db.Table<H22gLedgerRow>().AddRange(Rows());
        return db;
    }
}
