using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateDiffComputedRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DateTime Old { get; set; }
}

public class TimeSpanDifferenceReceiverAddTests
{
    private static List<DateDiffComputedRow> Rows() =>
    [
        new()
        {
            Id = 1,
            When = new DateTime(2024, 5, 10, 8, 0, 0),
            Old = new DateTime(2024, 5, 9, 6, 0, 0),
        },
        new()
        {
            Id = 2,
            When = new DateTime(2023, 1, 2, 3, 4, 5),
            Old = new DateTime(2023, 1, 1, 3, 4, 5),
        },
    ];

    private static TestDatabase Seed(bool textStorage)
    {
        TestDatabase db = textStorage
            ? new TestDatabase(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text))
            : new TestDatabase();
        db.Table<DateDiffComputedRow>().Schema.CreateTable();
        db.Table<DateDiffComputedRow>().AddRange(Rows());
        return db;
    }

    private static TimeSpan SpanFor(int id)
    {
        return TimeSpan.FromHours(id);
    }

    [Fact]
    public void AddStaticHelperSpanToDifferenceInSelectProjects()
    {
        using TestDatabase db = Seed(false);

        List<TimeSpan> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Subtract(r.Old).Add(SpanFor(r.Id)))
            .ToList();
        Assert.Equal([new TimeSpan(1, 3, 0, 0), new TimeSpan(1, 2, 0, 0)], expected);

        List<TimeSpan> actual = db.Table<DateDiffComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Subtract(r.Old).Add(SpanFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddConstantSpanToDifferenceTextStorageInSelectProjects()
    {
        using TestDatabase db = Seed(true);

        List<TimeSpan> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Subtract(r.Old).Add(TimeSpan.FromHours(1)))
            .ToList();
        Assert.Equal([new TimeSpan(1, 3, 0, 0), new TimeSpan(1, 1, 0, 0)], expected);

        List<TimeSpan> actual = db.Table<DateDiffComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.When.Subtract(r.Old).Add(TimeSpan.FromHours(1)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
