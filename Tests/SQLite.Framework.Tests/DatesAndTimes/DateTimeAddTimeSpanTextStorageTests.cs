using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TimeSpanTextScheduleRow
{
    [Key]
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }
}

public class DateTimeAddTimeSpanTextStorageTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<TimeSpanTextScheduleRow>().Schema.CreateTable();
        db.Table<TimeSpanTextScheduleRow>().Add(new TimeSpanTextScheduleRow { Id = 1, StartedAt = new DateTime(2024, 5, 10, 8, 30, 0) });
        db.Table<TimeSpanTextScheduleRow>().Add(new TimeSpanTextScheduleRow { Id = 2, StartedAt = new DateTime(2024, 5, 12, 3, 0, 0) });
        return db;
    }

    [Fact]
    public void AddMethodInSelectShiftsTheDate()
    {
        using TestDatabase db = SetupDatabase();
        TimeSpan span = new(1, 2, 3, 4);

        List<DateTime> expected = db.Table<TimeSpanTextScheduleRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt.Add(span))
            .ToList();

        Assert.Equal([new DateTime(2024, 5, 11, 10, 33, 4), new DateTime(2024, 5, 13, 5, 3, 4)], expected);

        List<DateTime> actual = db.Table<TimeSpanTextScheduleRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt.Add(span))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlusOperatorInSelectShiftsTheDate()
    {
        using TestDatabase db = SetupDatabase();
        TimeSpan span = new(1, 2, 3, 4);

        List<DateTime> expected = db.Table<TimeSpanTextScheduleRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt + span)
            .ToList();

        Assert.Equal([new DateTime(2024, 5, 11, 10, 33, 4), new DateTime(2024, 5, 13, 5, 3, 4)], expected);

        List<DateTime> actual = db.Table<TimeSpanTextScheduleRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt + span)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinusOperatorInSelectShiftsTheDate()
    {
        using TestDatabase db = SetupDatabase();
        TimeSpan span = new(1, 2, 3, 4);

        List<DateTime> expected = db.Table<TimeSpanTextScheduleRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt - span)
            .ToList();

        Assert.Equal([new DateTime(2024, 5, 9, 6, 26, 56), new DateTime(2024, 5, 11, 0, 56, 56)], expected);

        List<DateTime> actual = db.Table<TimeSpanTextScheduleRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.StartedAt - span)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlusOperatorInWhereKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        TimeSpan span = new(1, 2, 3, 4);
        DateTime target = new(2024, 5, 12, 12, 0, 0);

        List<int> expected = db.Table<TimeSpanTextScheduleRow>().AsEnumerable()
            .Where(r => r.StartedAt + span > target)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], expected);

        List<int> actual = db.Table<TimeSpanTextScheduleRow>()
            .Where(r => r.StartedAt + span > target)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
