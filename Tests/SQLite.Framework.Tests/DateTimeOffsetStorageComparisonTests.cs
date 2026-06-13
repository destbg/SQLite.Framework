using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeOffsetStorageComparisonTests
{
    private static readonly DateTimeOffset EarlierInstant = new(2020, 6, 1, 12, 0, 0, TimeSpan.FromHours(10));
    private static readonly DateTimeOffset LaterInstant = new(2020, 6, 1, 5, 0, 0, TimeSpan.Zero);

    private static List<StampedRow> Rows()
    {
        return new List<StampedRow>
        {
            new StampedRow { Id = 1, When = EarlierInstant },
            new StampedRow { Id = 2, When = LaterInstant },
        };
    }

    private static TestDatabase Seed(DateTimeOffsetStorageMode mode)
    {
        TestDatabase db = new(builder => builder.UseDateTimeOffsetStorage(mode));
        db.Table<StampedRow>().Schema.CreateTable();
        db.Table<StampedRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void TextFormattedEqualityMatchesSameInstantWithDifferentOffset()
    {
        using TestDatabase db = Seed(DateTimeOffsetStorageMode.TextFormatted);
        DateTimeOffset probe = EarlierInstant.ToOffset(TimeSpan.FromHours(2));

        int oracle = Rows().Count(e => e.When == probe);
        int actual = db.Table<StampedRow>().Count(e => e.When == probe);

        Assert.Equal(1, oracle);
        Assert.Equal(0, actual);
    }

    [Fact]
    public void TextFormattedOrderByOrdersByInstant()
    {
        using TestDatabase db = Seed(DateTimeOffsetStorageMode.TextFormatted);

        List<int> oracle = Rows().OrderBy(e => e.When).Select(e => e.Id).ToList();
        List<int> actual = db.Table<StampedRow>().OrderBy(e => e.When).Select(e => e.Id).ToList();

        Assert.Equal([1, 2], oracle);
        Assert.Equal([2, 1], actual);
    }

    [Fact]
    public void TicksEqualityRejectsDifferentInstantWithSameLocalClock()
    {
        using TestDatabase db = new(builder => builder.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.Ticks));
        db.Table<StampedRow>().Schema.CreateTable();
        List<StampedRow> rows = new()
        {
            new StampedRow { Id = 1, When = new DateTimeOffset(2020, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)) },
        };
        db.Table<StampedRow>().AddRange(rows);
        DateTimeOffset probe = new(2020, 6, 1, 10, 0, 0, TimeSpan.FromHours(5));

        int oracle = rows.Count(e => e.When == probe);
        int actual = db.Table<StampedRow>().Count(e => e.When == probe);

        Assert.Equal(0, oracle);
        Assert.Equal(1, actual);
    }

    [Fact]
    public void TicksDistinctKeepsDistinctInstants()
    {
        using TestDatabase db = new(builder => builder.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.Ticks));
        db.Table<StampedRow>().Schema.CreateTable();
        List<StampedRow> rows = new()
        {
            new StampedRow { Id = 1, When = new DateTimeOffset(2020, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)) },
            new StampedRow { Id = 2, When = new DateTimeOffset(2020, 6, 1, 10, 0, 0, TimeSpan.FromHours(5)) },
        };
        db.Table<StampedRow>().AddRange(rows);

        int oracle = rows.Select(e => e.When).Distinct().Count();
        int actual = db.Table<StampedRow>().Select(e => e.When).Distinct().ToList().Count;

        Assert.Equal(2, oracle);
        Assert.Equal(1, actual);
    }

    [Fact]
    public void TicksOrderByOrdersByInstant()
    {
        using TestDatabase db = Seed(DateTimeOffsetStorageMode.Ticks);

        List<int> oracle = Rows().OrderBy(e => e.When).Select(e => e.Id).ToList();
        List<int> actual = db.Table<StampedRow>().OrderBy(e => e.When).Select(e => e.Id).ToList();

        Assert.Equal([1, 2], oracle);
        Assert.Equal([2, 1], actual);
    }
}

public class StampedRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset When { get; set; }
}
