using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeSpanStorageTests
{
    [Fact]
    public void Integer_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7), result.Duration);
    }

    [Fact]
    public void Integer_Where_DaysEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Duration.Days == 2);

        Assert.NotNull(result);
    }

    [Fact]
    public void Integer_Where_HoursEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Duration.Hours == 3);

        Assert.NotNull(result);
    }

    [Fact]
    public void Integer_Select_Days()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int days = db.Table<TestEntity>().Select(a => a.Duration.Days).First();

        Assert.Equal(2, days);
    }

    [Fact]
    public void Integer_Select_Hours()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int hours = db.Table<TestEntity>().Select(a => a.Duration.Hours).First();

        Assert.Equal(3, hours);
    }

    [Fact]
    public void Integer_Select_Minutes()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int minutes = db.Table<TestEntity>().Select(a => a.Duration.Minutes).First();

        Assert.Equal(4, minutes);
    }

    [Fact]
    public void Integer_Select_Seconds()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int seconds = db.Table<TestEntity>().Select(a => a.Duration.Seconds).First();

        Assert.Equal(5, seconds);
    }

    [Fact]
    public void Integer_Select_TotalDays()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalDays = db.Table<TestEntity>().Select(a => a.Duration.TotalDays).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalDays, totalDays, 10);
    }

    [Fact]
    public void Integer_Select_TotalHours()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalHours = db.Table<TestEntity>().Select(a => a.Duration.TotalHours).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalHours, totalHours, 10);
    }

    [Fact]
    public void Integer_Select_TotalSeconds()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalSeconds = db.Table<TestEntity>().Select(a => a.Duration.TotalSeconds).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalSeconds, totalSeconds, 5);
    }

    [Fact]
    public void Text_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7), result.Duration);
    }

    [Fact]
    public void Text_RoundTrip_Negative()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(-1, -2, -3, -4, -5, -6) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new TimeSpan(-1, -2, -3, -4, -5, -6), result.Duration);
    }

    [Fact]
    public void Text_RoundTrip_Zero()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = TimeSpan.Zero });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(TimeSpan.Zero, result.Duration);
    }

    [Fact]
    public void Text_Where_DaysEquals_Throws()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TestEntity>().Where(a => a.Duration.Days == 2).ToList());
    }

    [Fact]
    public void Text_Where_HoursEquals_Throws()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TestEntity>().Where(a => a.Duration.Hours == 3).ToList());
    }

    [Fact]
    public void Text_Select_Days_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int days = db.Table<TestEntity>().Select(a => a.Duration.Days).First();

        Assert.Equal(2, days);
    }

    [Fact]
    public void Text_Select_Hours_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int hours = db.Table<TestEntity>().Select(a => a.Duration.Hours).First();

        Assert.Equal(3, hours);
    }

    [Fact]
    public void Text_Select_Minutes_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int minutes = db.Table<TestEntity>().Select(a => a.Duration.Minutes).First();

        Assert.Equal(4, minutes);
    }

    [Fact]
    public void Text_Select_Seconds_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        int seconds = db.Table<TestEntity>().Select(a => a.Duration.Seconds).First();

        Assert.Equal(5, seconds);
    }

    [Fact]
    public void Text_Select_TotalDays_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalDays = db.Table<TestEntity>().Select(a => a.Duration.TotalDays).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalDays, totalDays, 10);
    }

    [Fact]
    public void Text_Select_TotalHours_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalHours = db.Table<TestEntity>().Select(a => a.Duration.TotalHours).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalHours, totalHours, 10);
    }

    [Fact]
    public void Text_Select_TotalSeconds_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Duration = new TimeSpan(2, 3, 4, 5, 6, 7) });

        double totalSeconds = db.Table<TestEntity>().Select(a => a.Duration.TotalSeconds).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalSeconds, totalSeconds, 5);
    }

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.Table<TestEntity>().CreateTable();
        return db;
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required TimeSpan Duration { get; set; }
    }
}
