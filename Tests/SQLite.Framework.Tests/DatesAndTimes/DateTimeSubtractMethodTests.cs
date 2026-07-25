using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DateTimeSubtractRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Dt { get; set; }
}

internal sealed class DateTimeOffsetSubtractRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset Dto { get; set; }
}

public class DateTimeSubtractMethodTests
{
    private static readonly DateTimeSubtractRow[] DateTimeData =
    [
        new DateTimeSubtractRow { Id = 1, Dt = new DateTime(2024, 1, 10) },
        new DateTimeSubtractRow { Id = 2, Dt = new DateTime(2024, 1, 3) },
    ];

    [Fact]
    public void SubtractDateTimeReturnsTimeSpanLikeBinaryMinus()
    {
        using TestDatabase db = new();
        db.Table<DateTimeSubtractRow>().Schema.CreateTable();
        foreach (DateTimeSubtractRow r in DateTimeData)
        {
            db.Table<DateTimeSubtractRow>().Add(r);
        }

        DateTime baseDate = new(2024, 1, 1);

        List<int> expected = DateTimeData
            .Where(x => x.Dt.Subtract(baseDate) > TimeSpan.FromDays(5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<DateTimeSubtractRow>()
            .Where(x => x.Dt.Subtract(baseDate) > TimeSpan.FromDays(5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubtractTimeSpanReturnsDateTime()
    {
        using TestDatabase db = new();
        db.Table<DateTimeSubtractRow>().Schema.CreateTable();
        foreach (DateTimeSubtractRow r in DateTimeData)
        {
            db.Table<DateTimeSubtractRow>().Add(r);
        }

        TimeSpan span = TimeSpan.FromDays(1);
        DateTime targetDate = new(2024, 1, 9);

        List<int> expected = DateTimeData
            .Where(x => x.Dt.Subtract(span) == targetDate)
            .Select(x => x.Id)
            .ToList();

        List<int> actual = db.Table<DateTimeSubtractRow>()
            .Where(x => x.Dt.Subtract(span) == targetDate)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeOffsetSubtractDateTimeOffsetReturnsTimeSpan()
    {
        using TestDatabase db = new();
        db.Table<DateTimeOffsetSubtractRow>().Schema.CreateTable();
        DateTimeOffsetSubtractRow[] data =
        [
            new DateTimeOffsetSubtractRow { Id = 1, Dto = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero) },
            new DateTimeOffsetSubtractRow { Id = 2, Dto = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero) },
        ];
        foreach (DateTimeOffsetSubtractRow r in data)
        {
            db.Table<DateTimeOffsetSubtractRow>().Add(r);
        }

        DateTimeOffset baseDto = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        List<int> expected = data
            .Where(x => x.Dto.Subtract(baseDto) > TimeSpan.FromDays(5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<DateTimeOffsetSubtractRow>()
            .Where(x => x.Dto.Subtract(baseDto) > TimeSpan.FromDays(5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }
}
