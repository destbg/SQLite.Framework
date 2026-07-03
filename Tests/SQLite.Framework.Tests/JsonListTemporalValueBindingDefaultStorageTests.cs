using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<TimeSpan>))]
[JsonSerializable(typeof(List<DateTime>))]
internal partial class TemporalBindingListContext : JsonSerializerContext;

internal sealed class TemporalBindingRow
{
    [Key]
    public int Id { get; set; }

    public List<TimeSpan> Spans { get; set; } = [];

    public List<DateTime> Dates { get; set; } = [];
}

public class JsonListTemporalValueBindingDefaultStorageTests
{
    private static readonly DateTime DateA = new(2024, 5, 6, 7, 8, 9);

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
            Dates = [DateA]
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
}
