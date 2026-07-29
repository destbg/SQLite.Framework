using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lDecimalTextRows")]
public class H24lDecimalTextRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public decimal Other { get; set; }
}

public class DecimalTextToStringFilterParityTests
{
    [Fact]
    public void FiltersOnTheTextFormOfATextStoredDecimal()
    {
        string target = 10.50m.ToString();
        using TestDatabase db = Seed();
        List<H24lDecimalTextRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Amount.ToString() == target)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lDecimalTextRow>()
            .Where(r => r.Amount.ToString() == target)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnTheTextLengthOfATextStoredDecimal()
    {
        using TestDatabase db = Seed();
        List<H24lDecimalTextRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Amount.ToString().Length == 5)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lDecimalTextRow>()
            .Where(r => r.Amount.ToString().Length == 5)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnTheTextFormBesideAnotherDecimalComparison()
    {
        string target = 10.50m.ToString();
        using TestDatabase db = Seed();
        List<H24lDecimalTextRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Other > 3m && r.Amount.ToString() == target)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lDecimalTextRow>()
            .Where(r => r.Other > 3m && r.Amount.ToString() == target)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lDecimalTextRow> Rows()
    {
        return
        [
            new H24lDecimalTextRow { Id = 1, Amount = 10.50m, Other = 4m },
            new H24lDecimalTextRow { Id = 2, Amount = 7.25m, Other = 2m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H24lDecimalTextRow>().Schema.CreateTable();
        db.Table<H24lDecimalTextRow>().AddRange(Rows());
        return db;
    }
}
