using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeSpanTests
{
    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required TimeSpan Time { get; set; }
    }

    [Fact]
    public void AddToTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Time = a.Time.Add(TimeSpan.FromDays(1)),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new TimeSpan(3, 3, 4, 5, 6, 7), author.Time);
    }

    [Fact]
    public void AccessTimeSpanDaysDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int days = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Days
        ).First();

        Assert.Equal(2, days);
    }

    [Fact]
    public void AccessTimeSpanTotalDaysDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double days = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.TotalDays
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalDays, days);
    }

    [Fact]
    public void AccessTimeSpanHoursDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int hours = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Hours
        ).First();

        Assert.Equal(3, hours);
    }

    [Fact]
    public void AccessTimeSpanTotalHoursDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double hours = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.TotalHours
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalHours, hours);
    }

    [Fact]
    public void AccessTimeSpanMinutesDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int minutes = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Minutes
        ).First();

        Assert.Equal(4, minutes);
    }

    [Fact]
    public void AccessTimeSpanTotalMinutesDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double hours = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.TotalMinutes
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalMinutes, hours);
    }

    [Fact]
    public void AccessTimeSpanSecondsDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int seconds = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Seconds
        ).First();

        Assert.Equal(5, seconds);
    }

    [Fact]
    public void AccessTimeSpanMillisecondsDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double milliseconds = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Milliseconds
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).Milliseconds, milliseconds);
    }

    [Fact]
    public void AccessTimeSpanTotalMillisecondsDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double milliseconds = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.TotalMilliseconds
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalMilliseconds, milliseconds);
    }

    [Fact]
    public void AccessTimeSpanTotalSecondsDirectly()
    {
        using TestDatabase db = SetupDatabase();

        double seconds = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.TotalSeconds
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalSeconds, seconds);
    }

    [Fact]
    public void AccessTimeSpanTicksDirectly()
    {
        using TestDatabase db = SetupDatabase();

        long ticks = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Ticks
        ).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).Ticks, ticks);
    }

    [Fact]
    public void TimeSpanSubtract()
    {
        using TestDatabase db = SetupDatabase();

        TimeSpan compareTime = new(1, 0, 0, 0);
        long expectedTicks = new TimeSpan(1, 3, 4, 5, 6, 7).Ticks;

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time.Subtract(compareTime).Ticks == expectedTicks
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void TimeSpanNegate()
    {
        using TestDatabase db = SetupDatabase();

        long expectedTicks = -new TimeSpan(2, 3, 4, 5, 6, 7).Ticks;

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time.Negate().Ticks == expectedTicks
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void TimeSpanDuration()
    {
        using TestDatabase db = new();

        db.Table<TestEntity>().CreateTable();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Time = new TimeSpan(-2, -3, -4, -5, -6, -7)
        });

        long expectedTicks = new TimeSpan(2, 3, 4, 5, 6, 7).Ticks;

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time.Duration().Ticks == expectedTicks
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void TimeSpanFromDays()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromDays(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromDays(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromHours()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromHours(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromHours(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromMinutes()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromMinutes(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromMinutes(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromSeconds()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromSeconds(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromSeconds(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromMilliseconds()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromMilliseconds(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromMilliseconds(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromMicroseconds()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromMicroseconds(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromMicroseconds(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
    }

    [Fact]
    public void TimeSpanFromTicks()
    {
        using TestDatabase db = SetupDatabase();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = 2,
                Time = TimeSpan.FromTicks(2)
            }
        });

        TestEntity entity = (
            from a in db.Table<TestEntity>()
            where a.Time == TimeSpan.FromTicks(a.Id)
            select a
        ).First();

        Assert.NotNull(entity);
        Assert.Equal(2, entity.Id);
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
                Time = new TimeSpan(2, 3, 4, 5, 6, 7)
            }
        });

        return db;
    }
}