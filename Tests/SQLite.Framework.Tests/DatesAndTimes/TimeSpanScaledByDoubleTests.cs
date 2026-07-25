using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TimeSpanScaleRow
{
    [Key]
    public int Id { get; set; }

    public TimeSpan Span { get; set; }

    public TimeSpan? MaybeSpan { get; set; }
}

public class TimeSpanScaledByDoubleTests
{
    private static List<TimeSpanScaleRow> Rows() =>
    [
        new() { Id = 1, Span = new TimeSpan(2, 0, 0), MaybeSpan = new TimeSpan(2, 0, 0) },
        new() { Id = 2, Span = new TimeSpan(0, 30, 0), MaybeSpan = null },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<TimeSpanScaleRow>().Schema.CreateTable();
        db.Table<TimeSpanScaleRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void SelectSpanTimesDouble()
    {
        using TestDatabase db = Seed();

        List<TimeSpan> expected = Rows().OrderBy(r => r.Id).Select(r => r.Span * 2.5).ToList();
        Assert.Equal([new TimeSpan(5, 0, 0), new TimeSpan(1, 15, 0)], expected);

        List<TimeSpan> actual = db.Table<TimeSpanScaleRow>().OrderBy(r => r.Id).Select(r => r.Span * 2.5).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectSpanDividedByDouble()
    {
        using TestDatabase db = Seed();

        List<TimeSpan> expected = Rows().OrderBy(r => r.Id).Select(r => r.Span / 2.0).ToList();
        Assert.Equal([new TimeSpan(1, 0, 0), new TimeSpan(0, 15, 0)], expected);

        List<TimeSpan> actual = db.Table<TimeSpanScaleRow>().OrderBy(r => r.Id).Select(r => r.Span / 2.0).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectNullableSpanTimesDoubleKeepsNull()
    {
        using TestDatabase db = Seed();

        List<TimeSpan?> expected = Rows().OrderBy(r => r.Id).Select(r => r.MaybeSpan * 2.5).ToList();
        Assert.Equal([new TimeSpan(5, 0, 0), null], expected);

        List<TimeSpan?> actual = db.Table<TimeSpanScaleRow>().OrderBy(r => r.Id).Select(r => r.MaybeSpan * 2.5).ToList();
        Assert.Equal(expected, actual);
    }
}
