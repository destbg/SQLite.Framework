using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateOnlyTests
{
    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required DateOnly Date { get; set; }
    }

    [Fact]
    public void AddYearsToDateOnly()
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
        Assert.Equal(new DateOnly(7000, 2, 3), author.Date);
    }

    [Fact]
    public void AddMonthsToDateOnly()
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
        Assert.Equal(new DateOnly(2000, 4, 3), author.Date);
    }

    [Fact]
    public void AddDaysToDateOnly()
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
        Assert.Equal(new DateOnly(2000, 2, 13), author.Date);
    }

    [Fact]
    public void AccessDateOnlyYearDirectly()
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
    public void AccessDateOnlyMonthDirectly()
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
    public void AccessDateOnlyDayDirectly()
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
    public void AccessDateOnlyDayOfWeekDirectly()
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
    public void AccessDateOnlyDayOfYearDirectly()
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
                Date = new DateOnly(2000, 2, 3)
            }
        });

        return db;
    }
}