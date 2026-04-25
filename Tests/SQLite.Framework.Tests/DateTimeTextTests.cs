using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeTextTests
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss zz";

    [Fact]
    public void Read_WhenStoredAsTextTicks_ReturnsCorrectDateTime()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 1);

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8), entity.Date);
    }

    [Fact]
    public void AddToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.Add(TimeSpan.FromDays(1))
            }
        ).First();

        Assert.Equal(new DateTime(2000, 2, 4, 4, 5, 6, 7, 8), entity.Date);
    }

    [Fact]
    public void AddYearsToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddYears(5000)
            }
        ).First();

        Assert.Equal(
            new DateTime(7000, 2, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMonthsToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMonths(2)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 4, 3, 4, 5, 6, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddDaysToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddDays(10)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 13, 4, 5, 6, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddHoursToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddHours(5)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 3, 9, 5, 6, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMinutesToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMinutes(30)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 3, 4, 35, 6, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddSecondsToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddSeconds(45)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 3, 4, 5, 51, 7, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMillisecondsToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMilliseconds(2)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 3, 4, 5, 6, 9, 8).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddMicrosecondsToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddMicroseconds(2)
            }
        ).First();

        Assert.Equal(
            new DateTime(2000, 2, 3, 4, 5, 6, 7, 10).ToString(DateTimeFormat),
            entity.Date.ToString(DateTimeFormat));
    }

    [Fact]
    public void AddTicksToDateTime_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Date = a.Date.AddTicks(100)
            }
        ).First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8).Ticks + 100, entity.Date.Ticks);
    }

    [Fact]
    public void Year_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int year = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void Month_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int month = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void Day_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int day = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void Hour_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int hour = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void Minute_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int minute = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Minute).First();

        Assert.Equal(5, minute);
    }

    [Fact]
    public void Second_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int second = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Second).First();

        Assert.Equal(6, second);
    }

    [Fact]
    public void Millisecond_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int millisecond = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Millisecond).First();

        Assert.Equal(7, millisecond);
    }

    [Fact]
    public void Ticks_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        long ticks = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.Ticks).First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8).Ticks, ticks);
    }

    [Fact]
    public void DayOfWeek_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        DayOfWeek dayOfWeek = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.DayOfWeek).First();

        Assert.Equal(DayOfWeek.Thursday, dayOfWeek);
    }

    [Fact]
    public void DayOfYear_WhenStoredAsTextTicks()
    {
        using TestDatabase db = SetupTextTicksDatabase();

        int dayOfYear = (from a in db.Table<TestEntity>() where a.Id == 1 select a.Date.DayOfYear).First();

        Assert.Equal(34, dayOfYear);
    }

    [Fact]
    public void Read_WhenStoredAsFormattedString_ReturnsCorrectDateTime()
    {
        using TestDatabase db = SetupFormattedStringDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 1);

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6), entity.Date);
    }

    private static TestDatabase SetupTextTicksDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.Schema.CreateTable<TestEntity>();
        string ticks = new DateTime(2000, 2, 3, 4, 5, 6, 7, 8).Ticks.ToString();
        db.Execute("INSERT INTO TestEntity (Id, Date) VALUES (1, @date)",
            new SQLiteParameter
            {
                Name = "@date",
                Value = ticks
            });
        return db;
    }

    private static TestDatabase SetupFormattedStringDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.Schema.CreateTable<TestEntity>();
        db.Execute("INSERT INTO TestEntity (Id, Date) VALUES (1, @date)",
            new SQLiteParameter
            {
                Name = "@date",
                Value = "2000-02-03 04:05:06"
            });
        return db;
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required DateTime Date { get; set; }
    }
}