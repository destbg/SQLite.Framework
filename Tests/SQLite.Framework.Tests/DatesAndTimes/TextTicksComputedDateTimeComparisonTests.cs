using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TextTicksEventRow
{
    [Key]
    public int Id { get; set; }

    public DateTime OccurredAt { get; set; }
}

public class TextTicksComputedDateTimeComparisonTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextTicks));
        db.Table<TextTicksEventRow>().Schema.CreateTable();
        db.Table<TextTicksEventRow>().Add(new TextTicksEventRow { Id = 1, OccurredAt = new DateTime(2024, 5, 10, 8, 30, 0) });
        db.Table<TextTicksEventRow>().Add(new TextTicksEventRow { Id = 2, OccurredAt = new DateTime(2020, 1, 2, 3, 4, 5) });
        return db;
    }

    [Fact]
    public void DatePropertyEqualsConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        DateTime day = new(2024, 5, 10);

        List<int> expected = db.Table<TextTicksEventRow>().AsEnumerable()
            .Where(r => r.OccurredAt.Date == day)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<TextTicksEventRow>()
            .Where(r => r.OccurredAt.Date == day)
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }

    [Fact]
    public void AddDaysEqualsConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        DateTime next = new(2024, 5, 11, 8, 30, 0);

        List<int> expected = db.Table<TextTicksEventRow>().AsEnumerable()
            .Where(r => r.OccurredAt.AddDays(1) == next)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<TextTicksEventRow>()
            .Where(r => r.OccurredAt.AddDays(1) == next)
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }

    [Fact]
    public void DatePropertyLessThanConstantKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        DateTime day = new(2024, 5, 10);

        List<int> expected = db.Table<TextTicksEventRow>().AsEnumerable()
            .Where(r => r.OccurredAt.Date < day)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], expected);

        List<int> actual = db.Table<TextTicksEventRow>()
            .Where(r => r.OccurredAt.Date < day)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1, 2], actual);
    }
}
