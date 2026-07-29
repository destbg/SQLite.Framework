using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lDecimalEqualsRows")]
public class H24lDecimalEqualsRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public decimal Other { get; set; }
}

public class H24lDecimalEqualsFlag
{
    public int Id { get; set; }

    public bool Same { get; set; }
}

public class DecimalTextProjectionValueEqualityParityTests
{
    [Fact]
    public void ProjectsEqualsAgainstAConstantOfADifferentScale()
    {
        using TestDatabase db = Seed();
        List<H24lDecimalEqualsRow> local = Rows();

        List<bool> expected = local
            .OrderBy(r => r.Id)
            .Select(r => r.Amount.Equals(10.5m))
            .ToList();

        List<bool> actual = db.Table<H24lDecimalEqualsRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount.Equals(10.5m))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsEqualsBetweenTwoColumnsOfADifferentScale()
    {
        using TestDatabase db = Seed();
        List<H24lDecimalEqualsRow> local = Rows();

        List<bool> expected = local
            .OrderBy(r => r.Id)
            .Select(r => r.Amount.Equals(r.Other))
            .ToList();

        List<bool> actual = db.Table<H24lDecimalEqualsRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount.Equals(r.Other))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsEqualsInsideAConstructedProjection()
    {
        using TestDatabase db = Seed();
        List<H24lDecimalEqualsRow> local = Rows();

        List<(int Id, bool Same)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Same: r.Amount.Equals(r.Other)))
            .ToList();

        List<(int Id, bool Same)> actual = db.Table<H24lDecimalEqualsRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H24lDecimalEqualsFlag { Id = r.Id, Same = r.Amount.Equals(r.Other) })
            .AsEnumerable()
            .Select(x => (x.Id, x.Same))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsACapturedListContainsOfADifferentScale()
    {
        List<decimal> wanted = [10.5m, 3m];
        using TestDatabase db = Seed();
        List<H24lDecimalEqualsRow> local = Rows();

        List<bool> expected = local
            .OrderBy(r => r.Id)
            .Select(r => wanted.Contains(r.Amount))
            .ToList();

        List<bool> actual = db.Table<H24lDecimalEqualsRow>()
            .OrderBy(r => r.Id)
            .Select(r => wanted.Contains(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lDecimalEqualsRow> Rows()
    {
        return
        [
            new H24lDecimalEqualsRow { Id = 1, Amount = 10.50m, Other = 10.5m },
            new H24lDecimalEqualsRow { Id = 2, Amount = 3.000m, Other = 3m },
            new H24lDecimalEqualsRow { Id = 3, Amount = 7.25m, Other = 8.25m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H24lDecimalEqualsRow>().Schema.CreateTable();
        db.Table<H24lDecimalEqualsRow>().AddRange(Rows());
        return db;
    }
}
