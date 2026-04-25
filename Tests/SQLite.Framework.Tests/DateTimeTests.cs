using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeTests
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss zz";

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required DateTime Date { get; set; }
    }

    [Fact]
    public void AddToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 4, 4, 5, 6, 7, 8), author.Date);
    }

    [Fact]
    public void AddYearsToDateTime()
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
        Assert.Equal(new DateTime(7000, 2, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddRelativeYearsToDateTime()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddYears(a.Id),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTime(2001, 2, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMonthsToDateTime()
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
        Assert.Equal(new DateTime(2000, 4, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddRelativeMonthsToDateTime()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMonths(a.Id),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTime(2000, 3, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddDaysToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 13, 4, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddHoursToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 3, 9, 5, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMinutesToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 3, 4, 35, 6, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddSecondsToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 51, 7, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMillisecondsToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 9, 8).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMicrosecondsToDateTime()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMicroseconds(2),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 10).ToString(DateTimeFormat), author.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddTicksToDateTime()
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
        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8).Ticks + 100, author.Date.Ticks);
    }

    [Fact]
    public void AccessDateTimeYearDirectly()
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
    public void AccessDateTimeMonthDirectly()
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
    public void AccessDateTimeDayDirectly()
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
    public void AccessDateTimeHourDirectly()
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
    public void AccessDateTimeMinuteDirectly()
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
    public void AccessDateTimeSecondDirectly()
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
    public void AccessDateTimeMillisecondDirectly()
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
    public void AccessDateTimeTicksDirectly()
    {
        using TestDatabase db = SetupDatabase();

        long ticks = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Date.Ticks
        ).First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8).Ticks, ticks);
    }

    [Fact]
    public void AccessDateTimeDayOfWeekDirectly()
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
    public void AccessDateTimeDayOfYearDirectly()
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
                Date = new DateTime(2000, 2, 3, 4, 5, 6, 7, 8)
            }
        });

        return db;
    }
}