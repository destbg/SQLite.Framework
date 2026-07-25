using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedAddSpanRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public int Hours { get; set; }

    public TimeSpan Span { get; set; }

    public TimeSpan? MaybeSpan { get; set; }
}

public class DateTimeAddComputedTimeSpanTextStorageTests
{
    private static List<ComputedAddSpanRow> Rows() =>
    [
        new()
        {
            Id = 1,
            When = new DateTime(2024, 5, 10, 8, 0, 0),
            Hours = 30,
            Span = new TimeSpan(2, 0, 0),
            MaybeSpan = new TimeSpan(1, 30, 0),
        },
        new()
        {
            Id = 2,
            When = new DateTime(2024, 5, 11, 8, 0, 0),
            Hours = 1,
            Span = new TimeSpan(0, 15, 0),
            MaybeSpan = null,
        },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<ComputedAddSpanRow>().Schema.CreateTable();
        db.Table<ComputedAddSpanRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void AddFromHoursIntColumnInWhereFilters()
    {
        using TestDatabase db = Seed();
        DateTime cutoff = new(2024, 5, 11, 12, 0, 0);

        List<int> expected = Rows()
            .Where(r => r.When.Add(TimeSpan.FromHours(r.Hours)) > cutoff)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<ComputedAddSpanRow>()
            .Where(r => r.When.Add(TimeSpan.FromHours(r.Hours)) > cutoff)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddFromHoursIntColumnInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<DateTime> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Add(TimeSpan.FromHours(r.Hours)))
            .ToList();
        Assert.Equal([new DateTime(2024, 5, 11, 14, 0, 0), new DateTime(2024, 5, 11, 9, 0, 0)], expected);

        List<DateTime> actual = db.Table<ComputedAddSpanRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Add(TimeSpan.FromHours(r.Hours)))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddCoalescedNullableSpanColumnInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<DateTime> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Add(r.MaybeSpan ?? TimeSpan.Zero))
            .ToList();
        Assert.Equal([new DateTime(2024, 5, 10, 9, 30, 0), new DateTime(2024, 5, 11, 8, 0, 0)], expected);

        List<DateTime> actual = db.Table<ComputedAddSpanRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Add(r.MaybeSpan ?? TimeSpan.Zero))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddNegatedSpanColumnInWhereThrowsNotSupported()
    {
        using TestDatabase db = Seed();
        DateTime cutoff = new(2024, 5, 10, 7, 0, 0);

        Exception? ex = Record.Exception(() => db.Table<ComputedAddSpanRow>()
            .Where(r => r.When.Add(r.Span.Negate()) > cutoff)
            .Select(r => r.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }
}
