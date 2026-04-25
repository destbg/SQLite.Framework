using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeOffsetStorageTests
{
    [Fact]
    public void Ticks_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero), result.Date);
    }

    [Fact]
    public void Ticks_NonZeroOffset_PreservesLocalTime()
    {
        using TestDatabase db = SetupDatabase();
        DateTimeOffset original = new(2000, 2, 3, 4, 5, 6, TimeSpan.FromHours(5));
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = original
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(original.DateTime, result.Date.DateTime);
    }

    [Fact]
    public void Ticks_Where_YearEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Date.Year == 2000);

        Assert.NotNull(result);
    }

    [Fact]
    public void Ticks_Select_Year()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void Ticks_Select_Month()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void Ticks_Select_Day()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void Ticks_Select_Hour()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void UtcTicks_RoundTrip_PreservesUtcMoment()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        DateTimeOffset original = new(2000, 2, 3, 4, 5, 6, TimeSpan.FromHours(5));
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = original
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(original.UtcDateTime, result.Date.UtcDateTime);
    }

    [Fact]
    public void UtcTicks_Where_YearEquals()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        TestEntity? result = db.Table<TestEntity>().FirstOrDefault(a => a.Date.Year == 2000);

        Assert.NotNull(result);
    }

    [Fact]
    public void UtcTicks_Select_Year()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void UtcTicks_Select_Month()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void UtcTicks_Select_Day()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void UtcTicks_Select_Hour()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    [Fact]
    public void TextFormatted_RoundTrip_PreservesOffset()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        DateTimeOffset original = new(2000, 2, 3, 4, 5, 6, TimeSpan.FromHours(5));
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = original
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(original, result.Date);
    }

    [Fact]
    public void TextFormatted_Where_YearEquals_Throws()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        Assert.Throws<NotSupportedException>(() =>
            db.Table<TestEntity>().Where(a => a.Date.Year == 2000).ToList());
    }

    [Fact]
    public void TextFormatted_Select_Year_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int year = db.Table<TestEntity>().Select(a => a.Date.Year).First();

        Assert.Equal(2000, year);
    }

    [Fact]
    public void TextFormatted_Select_Month_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int month = db.Table<TestEntity>().Select(a => a.Date.Month).First();

        Assert.Equal(2, month);
    }

    [Fact]
    public void TextFormatted_Select_Day_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int day = db.Table<TestEntity>().Select(a => a.Date.Day).First();

        Assert.Equal(3, day);
    }

    [Fact]
    public void TextFormatted_Select_Hour_ComputesClientSide()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.Zero)
        });

        int hour = db.Table<TestEntity>().Select(a => a.Date.Hour).First();

        Assert.Equal(4, hour);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<TestEntity>().CreateTable();
        return db;
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required DateTimeOffset Date { get; set; }
    }
}