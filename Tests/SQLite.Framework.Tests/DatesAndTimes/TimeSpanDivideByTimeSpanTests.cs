using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TimeSpanRatioRow
{
    [Key]
    public int Id { get; set; }

    public TimeSpan A { get; set; }

    public TimeSpan B { get; set; }

    public TimeSpan? MaybeA { get; set; }

    public TimeSpan? MaybeB { get; set; }
}

public class TimeSpanDivideByTimeSpanTests
{
    private static List<TimeSpanRatioRow> Rows() =>
    [
        new() { Id = 1, A = new TimeSpan(3, 0, 0), B = new TimeSpan(2, 0, 0), MaybeA = new TimeSpan(3, 0, 0), MaybeB = new TimeSpan(2, 0, 0) },
        new() { Id = 2, A = new TimeSpan(1, 0, 0), B = new TimeSpan(4, 0, 0), MaybeA = null, MaybeB = null },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<TimeSpanRatioRow>().Schema.CreateTable();
        db.Table<TimeSpanRatioRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void SelectRatioReturnsFractionalResult()
    {
        using TestDatabase db = Seed();

        List<double> expected = Rows().OrderBy(r => r.Id).Select(r => r.A / r.B).ToList();
        Assert.Equal([1.5, 0.25], expected);

        List<double> actual = db.Table<TimeSpanRatioRow>().OrderBy(r => r.Id).Select(r => r.A / r.B).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereRatioAboveFractionalThreshold()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.A / r.B > 1.2).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<TimeSpanRatioRow>()
            .Where(r => r.A / r.B > 1.2).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectNullableRatioKeepsNull()
    {
        using TestDatabase db = Seed();

        List<double?> expected = Rows().OrderBy(r => r.Id).Select(r => r.MaybeA / r.MaybeB).ToList();
        Assert.Equal([1.5, null], expected);

        List<double?> actual = db.Table<TimeSpanRatioRow>().OrderBy(r => r.Id).Select(r => r.MaybeA / r.MaybeB).ToList();
        Assert.Equal(expected, actual);
    }
}
