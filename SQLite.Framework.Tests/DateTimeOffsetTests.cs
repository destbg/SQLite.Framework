using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeOffsetTests
{
    private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss zz";

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required DateTimeOffset Date { get; set; }
    }

    [Fact]
    public void AddToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.Add(TimeSpan.FromDays(1)),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 4, 4, 5, 6, 7, 8, TimeSpan.Zero), author.Date);
    }

    [Fact]
    public void AddYearsToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddYears(5000),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(7000, 2, 3, 4, 5, 6, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddMonthsToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMonths(2),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 4, 3, 4, 5, 6, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddDaysToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddDays(10),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 13, 4, 5, 6, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddHoursToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddHours(5),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 3, 9, 5, 6, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddMinutesToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMinutes(30),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 35, 6, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddSecondsToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddSeconds(45),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 51, 7, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddMillisecondsToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMilliseconds(2),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 6, 9, 8, TimeSpan.Zero).ToString(DateTimeOffsetFormat), author.Date.ToString(DateTimeOffsetFormat));
    }

    [Fact]
    public void AddTicksToDateTimeOffset()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddTicks(100),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 6, 7, 8, TimeSpan.Zero).Ticks + 100, author.Date.Ticks);
    }

    [Fact]
    public void AccessDateTimeOffsetYearDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int year = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Year
        ).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void AccessDateTimeOffsetMonthDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int month = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Month
        ).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void AccessDateTimeOffsetDayDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int day = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Day
        ).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void AccessDateTimeOffsetHourDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int hour = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Hour
        ).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void AccessDateTimeOffsetMinuteDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int minute = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Minute
        ).First();

        Assert.Equal(5, minute);
    }

    [Fact]
    public void AccessDateTimeOffsetSecondDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int second = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Second
        ).First();

        Assert.Equal(6, second);
    }

    [Fact]
    public void AccessDateTimeOffsetMillisecondDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int millisecond = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Millisecond
        ).First();

        Assert.Equal(7, millisecond);
    }

    [Fact]
    public void AccessDateTimeOffsetTicksDirectly()
    {
        using TestDatabase db = SetupDatabase();

        long ticks = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Ticks
        ).First();

        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 6, 7, 8, TimeSpan.Zero).Ticks, ticks);
    }

    [Fact]
    public void AccessDateTimeOffsetDayOfWeekDirectly()
    {
        using TestDatabase db = SetupDatabase();

        DayOfWeek dayOfWeek = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.DayOfWeek
        ).First();

        Assert.Equal(DayOfWeek.Thursday, dayOfWeek);
    }

    [Fact]
    public void AccessDateTimeOffsetDayOfYearDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int dayOfYear = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.DayOfYear
        ).First();

        Assert.Equal(34, dayOfYear);
    }

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<TestEntity>().CreateTable();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 1,
                Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, 7, 8, TimeSpan.Zero)
            }
        });

        return db;
    }
}