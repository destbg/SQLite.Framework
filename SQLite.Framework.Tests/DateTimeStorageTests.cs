using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeStorageTests
{
    [Fact]
    public void Integer_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6, 7, 8) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8), result.Date);
    }

    [Fact]
    public void Integer_Where_YearEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3) });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Date.Year == 2000);

        Assert.NotNull(result);
    }

    [Fact]
    public void Integer_Select_Year()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void Integer_Select_Month()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void Integer_Select_Day()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void Integer_Select_Hour()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void Integer_Select_Minute()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int minute = db.Table<TestEntity>().Select(a => a.Date.Minute).First();

        Assert.Equal(5, minute);
    }

    [Fact]
    public void Integer_Select_Second()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int second = db.Table<TestEntity>().Select(a => a.Date.Second).First();

        Assert.Equal(6, second);
    }

    [Fact]
    public void TextTicks_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6, 7, 8) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6, 7, 8), result.Date);
    }

    [Fact]
    public void TextTicks_Where_YearEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3) });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Date.Year == 2000);

        Assert.NotNull(result);
    }

    [Fact]
    public void TextTicks_Select_Year()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void TextTicks_Select_Month()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void TextTicks_Select_Day()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void TextTicks_Select_Hour()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void TextTicks_Select_Minute()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int minute = db.Table<TestEntity>().Select(a => a.Date.Minute).First();

        Assert.Equal(5, minute);
    }

    [Fact]
    public void TextTicks_Select_Second()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextTicks;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int second = db.Table<TestEntity>().Select(a => a.Date.Second).First();

        Assert.Equal(6, second);
    }

    [Fact]
    public void TextFormatted_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6), result.Date);
    }

    [Fact]
    public void TextFormatted_Where_YearEquals_Throws()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TestEntity>().Where(a => a.Date.Year == 2000).ToList());
    }

    [Fact]
    public void TextFormatted_Select_Year_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void TextFormatted_Select_Month_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void TextFormatted_Select_Day_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void TextFormatted_Select_Hour_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void TextFormatted_Select_Minute_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int minute = db.Table<TestEntity>().Select(a => a.Date.Minute).First();

        Assert.Equal(5, minute);
    }

    [Fact]
    public void TextFormatted_Select_Second_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Date = new DateTime(2000, 2, 3, 4, 5, 6) });

        int second = db.Table<TestEntity>().Select(a => a.Date.Second).First();

        Assert.Equal(6, second);
    }

    [Fact]
    public void TextColumn_TicksAsString_RoundTrip()
    {
        using TestDatabase db = new();
        DateTime expected = new(2024, 6, 15, 12, 30, 0);
        db.Execute("CREATE TABLE TestEntity (Id INTEGER PRIMARY KEY, Date TEXT NOT NULL)");
        db.Execute($"INSERT INTO TestEntity (Id, Date) VALUES (1, '{expected.Ticks}')");

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(expected, result.Date);
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

        public required DateTime Date { get; set; }
    }
}
