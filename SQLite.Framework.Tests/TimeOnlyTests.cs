using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeOnlyTests
{
    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required TimeOnly Time { get; set; }
    }

    [Fact]
    public void AddToTimeOnly()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select new TestEntity
            {
                Id = a.Id,
                Time = a.Time.Add(TimeSpan.FromHours(1)),
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal(new TimeOnly(4, 4, 5, 6, 7), author.Time);
    }

    [Fact]
    public void AccessTimeOnlyHoursDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int hours = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Hour
        ).First();

        Assert.Equal(3, hours);
    }

    [Fact]
    public void AccessTimeOnlyMinutesDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int minutes = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Minute
        ).First();

        Assert.Equal(4, minutes);
    }

    [Fact]
    public void AccessTimeOnlySecondsDirectly()
    {
        using TestDatabase db = SetupDatabase();

        int seconds = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Second
        ).First();

        Assert.Equal(5, seconds);
    }

    [Fact]
    public void AccessTimeOnlyTicksDirectly()
    {
        using TestDatabase db = SetupDatabase();

        long ticks = (
            from a in db.Table<TestEntity>()
            where a.Id == 1
            select a.Time.Ticks
        ).First();

        Assert.Equal(new TimeOnly(3, 4, 5, 6, 7).Ticks, ticks);
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
                Time = new TimeOnly(3, 4, 5, 6, 7)
            }
        });

        return db;
    }
}