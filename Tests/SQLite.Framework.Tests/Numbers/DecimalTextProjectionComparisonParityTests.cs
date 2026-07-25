using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mPriceRows")]
public class H21mPriceRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class H21mPriceFlag
{
    public int Id { get; set; }

    public bool Cheap { get; set; }
}

public class DecimalTextProjectionComparisonParityTests
{
    [Fact]
    public void AnonymousMemberLessThanConstantComparesByValue()
    {
        using TestDatabase db = Seed();
        List<H21mPriceRow> local = Rows();

        List<(int Id, bool Cheap)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Cheap: r.Amount < 10m))
            .ToList();

        List<(int Id, bool Cheap)> actual = db.Table<H21mPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Cheap = r.Amount < 10m })
            .AsEnumerable()
            .Select(x => (x.Id, x.Cheap))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnonymousMemberGreaterThanConstantComparesByValue()
    {
        using TestDatabase db = Seed();
        List<H21mPriceRow> local = Rows();

        List<(int Id, bool Big)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Big: r.Amount > 10m))
            .ToList();

        List<(int Id, bool Big)> actual = db.Table<H21mPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Big = r.Amount > 10m })
            .AsEnumerable()
            .Select(x => (x.Id, x.Big))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberInitMemberLessThanConstantComparesByValue()
    {
        using TestDatabase db = Seed();
        List<H21mPriceRow> local = Rows();

        List<(int Id, bool Cheap)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Cheap: r.Amount < 10m))
            .ToList();

        List<(int Id, bool Cheap)> actual = db.Table<H21mPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H21mPriceFlag { Id = r.Id, Cheap = r.Amount < 10m })
            .AsEnumerable()
            .Select(x => (x.Id, x.Cheap))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarBodyLessThanConstantComparesByValue()
    {
        using TestDatabase db = Seed();
        List<H21mPriceRow> local = Rows();

        List<bool> expected = local
            .OrderBy(r => r.Id)
            .Select(r => r.Amount < 10m)
            .ToList();

        List<bool> actual = db.Table<H21mPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount < 10m)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21mPriceRow> Rows()
    {
        return
        [
            new H21mPriceRow { Id = 1, Amount = 9.99m },
            new H21mPriceRow { Id = 2, Amount = 10.11m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H21mPriceRow>().Schema.CreateTable();
        db.Table<H21mPriceRow>().AddRange(Rows());
        return db;
    }
}
