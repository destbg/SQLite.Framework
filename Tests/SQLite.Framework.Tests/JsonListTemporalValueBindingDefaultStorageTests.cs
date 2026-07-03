using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<TimeSpan>))]
[JsonSerializable(typeof(List<DateTime>))]
[JsonSerializable(typeof(List<DateTimeOffset>))]
[JsonSerializable(typeof(List<DateOnly>))]
[JsonSerializable(typeof(List<TimeOnly>))]
internal partial class TemporalBindingListContext : JsonSerializerContext;

internal sealed class TemporalBindingRow
{
    [Key]
    public int Id { get; set; }

    public List<TimeSpan> Spans { get; set; } = [];

    public List<DateTime> Dates { get; set; } = [];

    public List<DateTimeOffset> Offsets { get; set; } = [];

    public List<DateOnly> Days { get; set; } = [];

    public List<TimeOnly> Times { get; set; } = [];
}

public class JsonListTemporalValueBindingDefaultStorageTests
{
    private static readonly DateTime DateA = new(2024, 5, 6, 7, 8, 9);
    private static readonly DateTime DateUtcFractional = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1_230_000);
    private static readonly DateTime DateLocal = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Local);
    private static readonly DateTimeOffset OffsetWhole = new(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset OffsetFractional = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2)).AddTicks(4_560_000);
    private static readonly DateOnly DayA = new(2024, 5, 6);
    private static readonly TimeOnly TimeWhole = new(7, 8, 9);
    private static readonly TimeOnly TimeFractional = new TimeOnly(7, 8, 9).Add(TimeSpan.FromTicks(7_890_000));

    private static TestDatabase Seed(Action<SQLiteOptionsBuilder>? extra = null)
    {
        TestDatabase db = new(b =>
        {
            b.AddJsonContext(TemporalBindingListContext.Default);
            extra?.Invoke(b);
        });
        db.Table<TemporalBindingRow>().Schema.CreateTable();
        db.Table<TemporalBindingRow>().Add(new TemporalBindingRow
        {
            Id = 1,
            Spans = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)],
            Dates = [DateA, DateUtcFractional, DateLocal],
            Offsets = [OffsetWhole, OffsetFractional],
            Days = [DayA],
            Times = [TimeWhole, TimeFractional]
        });
        return db;
    }

    [Fact]
    public void TimeSpanListContainsDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<TimeSpan> spans = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)];
        bool expected = spans.Contains(TimeSpan.FromMinutes(1));
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>()
            .Select(r => r.Spans.Contains(TimeSpan.FromMinutes(1)))
            .First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeSpanListContainsTextStorage()
    {
        using TestDatabase db = Seed(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));

        List<TimeSpan> spans = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)];
        bool expected = spans.Contains(TimeSpan.FromMinutes(1));
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>()
            .Select(r => r.Spans.Contains(TimeSpan.FromMinutes(1)))
            .First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeListContainsDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTime> dates = [DateA];
        bool expected = dates.Contains(DateA);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Dates.Contains(DateA)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeListFirstEqualsDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTime> dates = [DateA];
        bool expected = dates.First() == DateA;
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Dates.First() == DateA)
            .First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeSpanListFirstEqualsComputedSpanDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<TimeSpan> spans = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)];
        bool expected = spans.First() == TimeSpan.FromMinutes(1);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Spans.First() == TimeSpan.FromMinutes(1))
            .First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeListContainsUtcFractionalDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTime> dates = [DateA, DateUtcFractional, DateLocal];
        bool expected = dates.Contains(DateUtcFractional);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Dates.Contains(DateUtcFractional)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeListContainsLocalKindDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTime> dates = [DateA, DateUtcFractional, DateLocal];
        bool expected = dates.Contains(DateLocal);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Dates.Contains(DateLocal)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeOffsetListContainsWholeSecondDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTimeOffset> offsets = [OffsetWhole, OffsetFractional];
        bool expected = offsets.Contains(OffsetWhole);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Offsets.Contains(OffsetWhole)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeOffsetListContainsFractionalDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateTimeOffset> offsets = [OffsetWhole, OffsetFractional];
        bool expected = offsets.Contains(OffsetFractional);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Offsets.Contains(OffsetFractional)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateOnlyListContainsDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<DateOnly> days = [DayA];
        bool expected = days.Contains(DayA);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Days.Contains(DayA)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeOnlyListContainsWholeSecondDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<TimeOnly> times = [TimeWhole, TimeFractional];
        bool expected = times.Contains(TimeWhole);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Times.Contains(TimeWhole)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeOnlyListContainsFractionalDefaultStorage()
    {
        using TestDatabase db = Seed();

        List<TimeOnly> times = [TimeWhole, TimeFractional];
        bool expected = times.Contains(TimeFractional);
        Assert.True(expected);

        bool actual = db.Table<TemporalBindingRow>().Select(r => r.Times.Contains(TimeFractional)).First();
        Assert.Equal(expected, actual);
    }
}
