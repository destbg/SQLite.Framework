using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mInvoiceRows")]
public class H21mInvoiceRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextSetOperationOrderingTests
{
    [Fact]
    public void OrderByDecimalColumnAfterUnionReturnsRows()
    {
        using TestDatabase db = Seed();
        List<H21mInvoiceRow> local = Rows();

        List<decimal> expected = local.Where(r => r.Id == 1)
            .Union(local.Where(r => r.Id == 2))
            .OrderBy(r => r.Amount)
            .Select(r => r.Amount)
            .ToList();

        List<decimal> actual = db.Table<H21mInvoiceRow>().Where(r => r.Id == 1)
            .Union(db.Table<H21mInvoiceRow>().Where(r => r.Id == 2))
            .OrderBy(r => r.Amount)
            .AsEnumerable()
            .Select(r => r.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByIntegerColumnAfterUnionReturnsRows()
    {
        using TestDatabase db = Seed();
        List<H21mInvoiceRow> local = Rows();

        List<int> expected = local.Where(r => r.Id == 1)
            .Union(local.Where(r => r.Id == 2))
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21mInvoiceRow>().Where(r => r.Id == 1)
            .Union(db.Table<H21mInvoiceRow>().Where(r => r.Id == 2))
            .OrderBy(r => r.Id)
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21mInvoiceRow> Rows()
    {
        return
        [
            new H21mInvoiceRow { Id = 1, Amount = 10.11m },
            new H21mInvoiceRow { Id = 2, Amount = 9.99m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H21mInvoiceRow>().Schema.CreateTable();
        db.Table<H21mInvoiceRow>().AddRange(Rows());
        return db;
    }
}
