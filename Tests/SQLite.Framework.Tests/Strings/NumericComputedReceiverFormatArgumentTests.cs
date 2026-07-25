using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NumericComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public double Ratio { get; set; }
}

public class NumericComputedReceiverFormatArgumentTests
{
    private static List<NumericComputedRow> Rows() =>
    [
        new() { Id = 1, Amount = 3, Ratio = 1.5 },
        new() { Id = 2, Amount = 7, Ratio = 2.5 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NumericComputedRow>().Schema.CreateTable();
        db.Table<NumericComputedRow>().AddRange(Rows());
        return db;
    }

    private static string FormatFor(int id)
    {
        return id == 1 ? "D3" : "D5";
    }

    private static string FloatFormatFor(int id)
    {
        return "F1";
    }

    [Fact]
    public void IntegerToStringStaticHelperFormatOfProductInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).ToString(FormatFor(r.Id)))
            .ToList();
        Assert.Equal(["006", "00014"], expected);

        List<string> actual = db.Table<NumericComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).ToString(FormatFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleToStringStaticHelperFormatOfProductInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Ratio * 2.0).ToString(FloatFormatFor(r.Id)))
            .ToList();
        Assert.Equal(["3.0", "5.0"], expected);

        List<string> actual = db.Table<NumericComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Ratio * 2.0).ToString(FloatFormatFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
