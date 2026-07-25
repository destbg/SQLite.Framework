using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TemporalComputedRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset Offset { get; set; }

    public TimeSpan Span { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }
}

public class TemporalComputedReceiverProjectionTests
{
    private static List<TemporalComputedRow> Rows() =>
    [
        new()
        {
            Id = 1,
            Offset = new DateTimeOffset(2024, 5, 10, 8, 0, 0, TimeSpan.Zero),
            Span = new TimeSpan(2, 30, 0),
            Date = new DateOnly(2024, 5, 10),
            Time = new TimeOnly(8, 15, 0),
        },
        new()
        {
            Id = 2,
            Offset = new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.Zero),
            Span = new TimeSpan(0, 45, 0),
            Date = new DateOnly(2023, 1, 2),
            Time = new TimeOnly(20, 5, 0),
        },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<TemporalComputedRow>().Schema.CreateTable();
        db.Table<TemporalComputedRow>().AddRange(Rows());
        return db;
    }

    private static int HoursFor(int id)
    {
        return id * 2;
    }

    [Fact]
    public void OffsetAddHoursStaticHelperAfterAddYearsInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<DateTimeOffset> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Offset.AddYears(1).AddHours(HoursFor(r.Id)))
            .ToList();
        Assert.Equal(
        [
            new DateTimeOffset(2025, 5, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 2, 7, 4, 5, TimeSpan.Zero),
        ], expected);

        List<DateTimeOffset> actual = db.Table<TemporalComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Offset.AddYears(1).AddHours(HoursFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SpanToStringOfAddInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Span.Add(TimeSpan.FromHours(1)).ToString())
            .ToList();
        Assert.Equal(["03:30:00", "01:45:00"], expected);

        List<string> actual = db.Table<TemporalComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Span.Add(TimeSpan.FromHours(1)).ToString())
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateToStringOfAddYearsInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Date.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(["2025-05-10", "2024-01-02"], expected);

        List<string> actual = db.Table<TemporalComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Date.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeToStringOfAddHoursInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Time.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(["09:15", "21:05"], expected);

        List<string> actual = db.Table<TemporalComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Time.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
