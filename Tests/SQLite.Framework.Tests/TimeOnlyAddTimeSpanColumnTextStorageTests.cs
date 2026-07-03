using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TimeOnlyAddSpanRow
{
    [Key]
    public int Id { get; set; }

    public TimeOnly Time { get; set; }

    public DateTime When { get; set; }

    public DateTimeOffset WhenOffset { get; set; }

    public TimeSpan Span { get; set; }
}

public class TimeOnlyAddTimeSpanColumnTextStorageTests
{
    private static List<TimeOnlyAddSpanRow> Rows() =>
    [
        new()
        {
            Id = 1,
            Time = new TimeOnly(10, 30),
            When = new DateTime(2024, 5, 10, 10, 30, 0),
            WhenOffset = new DateTimeOffset(2024, 5, 10, 10, 30, 0, TimeSpan.Zero),
            Span = new TimeSpan(2, 0, 0),
        },
        new()
        {
            Id = 2,
            Time = new TimeOnly(1, 49),
            When = new DateTime(2024, 5, 12, 1, 49, 0),
            WhenOffset = new DateTimeOffset(2024, 5, 12, 1, 49, 0, TimeSpan.Zero),
            Span = new TimeSpan(2, 0, 0),
        },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<TimeOnlyAddSpanRow>().Schema.CreateTable();
        db.Table<TimeOnlyAddSpanRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void AddSpanColumnReturnsShiftedTime()
    {
        using TestDatabase db = Seed();

        List<TimeOnly> expected = Rows().OrderBy(r => r.Id).Select(r => r.Time.Add(r.Span)).ToList();
        Assert.Equal([new TimeOnly(12, 30), new TimeOnly(3, 49)], expected);

        List<TimeOnly> actual = db.Table<TimeOnlyAddSpanRow>().OrderBy(r => r.Id).Select(r => r.Time.Add(r.Span)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddConstantSpanReturnsShiftedTime()
    {
        using TestDatabase db = Seed();
        TimeSpan shift = new(1, 15, 0);

        List<TimeOnly> expected = Rows().OrderBy(r => r.Id).Select(r => r.Time.Add(shift)).ToList();
        Assert.Equal([new TimeOnly(11, 45), new TimeOnly(3, 4)], expected);

        List<TimeOnly> actual = db.Table<TimeOnlyAddSpanRow>().OrderBy(r => r.Id).Select(r => r.Time.Add(shift)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddSpanColumnInWhereThrows()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<TimeOnlyAddSpanRow>()
            .Where(r => r.Time.Add(r.Span) > new TimeOnly(12, 0)).Select(r => r.Id).ToList());
        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void DateTimeAddSpanColumnReturnsShiftedInstant()
    {
        using TestDatabase db = Seed();

        List<DateTime> expected = Rows().OrderBy(r => r.Id).Select(r => r.When.Add(r.Span)).ToList();
        Assert.Equal([new DateTime(2024, 5, 10, 12, 30, 0), new DateTime(2024, 5, 12, 3, 49, 0)], expected);

        List<DateTime> actual = db.Table<TimeOnlyAddSpanRow>().OrderBy(r => r.Id).Select(r => r.When.Add(r.Span)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeOffsetAddSpanColumnReturnsShiftedInstant()
    {
        using TestDatabase db = Seed();

        List<DateTimeOffset> expected = Rows().OrderBy(r => r.Id).Select(r => r.WhenOffset.Add(r.Span)).ToList();
        Assert.Equal(
        [
            new DateTimeOffset(2024, 5, 10, 12, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 12, 3, 49, 0, TimeSpan.Zero),
        ], expected);

        List<DateTimeOffset> actual = db.Table<TimeOnlyAddSpanRow>().OrderBy(r => r.Id).Select(r => r.WhenOffset.Add(r.Span)).ToList();
        Assert.Equal(expected, actual);
    }
}
