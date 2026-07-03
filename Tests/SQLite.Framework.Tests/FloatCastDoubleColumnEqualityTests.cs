using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatCastDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double D { get; set; }
}

public class FloatCastDoubleColumnEqualityTests
{
    private static List<FloatCastDoubleRow> Rows() =>
    [
        new() { Id = 1, D = 0.1 },
        new() { Id = 2, D = 0.5 },
        new() { Id = 3, D = 1e300 },
    ];

    [Fact]
    public void FloatCastOfDoubleColumnEqualsFloatConstant()
    {
        using TestDatabase db = new();
        db.Table<FloatCastDoubleRow>().Schema.CreateTable();
        db.Table<FloatCastDoubleRow>().AddRange(Rows());

        List<int> expected = Rows().Where(r => (float)r.D == 0.1f).Select(r => r.Id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<FloatCastDoubleRow>().Where(r => (float)r.D == 0.1f).Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FloatCastOfDoubleColumnProjectsLikeDotnet()
    {
        using TestDatabase db = new();
        db.Table<FloatCastDoubleRow>().Schema.CreateTable();
        db.Table<FloatCastDoubleRow>().AddRange(Rows());

        List<float> expected = Rows().OrderBy(r => r.Id).Select(r => (float)r.D).ToList();
        Assert.Equal([0.1f, 0.5f, float.PositiveInfinity], expected);

        List<float> actual = db.Table<FloatCastDoubleRow>().OrderBy(r => r.Id).Select(r => (float)r.D).ToList();
        Assert.Equal(expected, actual);
    }
}
