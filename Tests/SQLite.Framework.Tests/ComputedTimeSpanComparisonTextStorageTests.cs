using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TimeSpanTextShiftRow
{
    [Key]
    public int Id { get; set; }

    public DateTime LoggedAt { get; set; }
}

public class ComputedTimeSpanComparisonTextStorageTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.UseTimeSpanStorage(TimeSpanStorageMode.Text));
        db.Table<TimeSpanTextShiftRow>().Schema.CreateTable();
        db.Table<TimeSpanTextShiftRow>().Add(new TimeSpanTextShiftRow { Id = 1, LoggedAt = new DateTime(2024, 5, 10, 8, 30, 0) });
        db.Table<TimeSpanTextShiftRow>().Add(new TimeSpanTextShiftRow { Id = 2, LoggedAt = new DateTime(2024, 5, 12, 3, 0, 0) });
        return db;
    }

    [Fact]
    public void TimeOfDayEqualsConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        TimeSpan timeOfDay = new(8, 30, 0);

        List<int> expected = db.Table<TimeSpanTextShiftRow>().AsEnumerable()
            .Where(r => r.LoggedAt.TimeOfDay == timeOfDay)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<TimeSpanTextShiftRow>()
            .Where(r => r.LoggedAt.TimeOfDay == timeOfDay)
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }

    [Fact]
    public void TimeOfDayGreaterThanConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<TimeSpanTextShiftRow>().AsEnumerable()
            .Where(r => r.LoggedAt.TimeOfDay > TimeSpan.FromHours(5))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<TimeSpanTextShiftRow>()
            .Where(r => r.LoggedAt.TimeOfDay > TimeSpan.FromHours(5))
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }

    [Fact]
    public void DateTimeSubtractionComparedToConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        DateTime baseDate = new(2024, 5, 9);

        List<int> expected = db.Table<TimeSpanTextShiftRow>().AsEnumerable()
            .Where(r => r.LoggedAt - baseDate > TimeSpan.FromDays(2))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], expected);

        List<int> actual = db.Table<TimeSpanTextShiftRow>()
            .Where(r => r.LoggedAt - baseDate > TimeSpan.FromDays(2))
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }
}
