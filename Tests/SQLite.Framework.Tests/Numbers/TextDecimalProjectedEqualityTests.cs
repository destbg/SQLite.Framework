using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23jScaledPriceRows")]
public class H23jScaledPriceRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class TextDecimalProjectedEqualityTests
{
    [Fact]
    public void ProjectedEqualityAgainstADifferentScaleMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ProjectedEqualityAgainstADifferentScaleMatchesLinq));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount == 10.00m)
            .ToList();

        List<bool> actual = db.Table<H23jScaledPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount == 10.00m)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedInequalityAgainstADifferentScaleMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ProjectedInequalityAgainstADifferentScaleMatchesLinq));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount != 10.00m)
            .ToList();

        List<bool> actual = db.Table<H23jScaledPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Amount != 10.00m)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualityInsideAConstructedProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(EqualityInsideAConstructedProjectionMatchesLinq));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Same = r.Amount == 10.00m })
            .Select(x => x.Same)
            .ToList();

        List<bool> actual = db.Table<H23jScaledPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Same = r.Amount == 10.00m })
            .Select(x => x.Same)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InequalityInsideAConstructedProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(InequalityInsideAConstructedProjectionMatchesLinq));

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Other = r.Amount != 10.00m })
            .Select(x => x.Other)
            .ToList();

        List<bool> actual = db.Table<H23jScaledPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Other = r.Amount != 10.00m })
            .Select(x => x.Other)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedEqualityAgreesWithFilteredEquality()
    {
        using TestDatabase db = Setup(nameof(ProjectedEqualityAgreesWithFilteredEquality));

        List<int> filtered = db.Table<H23jScaledPriceRow>()
            .Where(r => r.Amount == 10.00m)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> projected = db.Table<H23jScaledPriceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Same = r.Amount == 10.00m })
            .AsEnumerable()
            .Where(x => x.Same)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(filtered, projected);
    }

    private static List<H23jScaledPriceRow> Rows()
    {
        return
        [
            new H23jScaledPriceRow { Id = 1, Amount = 10.0m },
            new H23jScaledPriceRow { Id = 2, Amount = 2.50m }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<H23jScaledPriceRow>().Schema.CreateTable();
        db.Table<H23jScaledPriceRow>().AddRange(Rows());
        return db;
    }
}
