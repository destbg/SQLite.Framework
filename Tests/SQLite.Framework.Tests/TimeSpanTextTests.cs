using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeSpanTextTests
{
    [Fact]
    public void Read_WhenStoredAsTextCFormat_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 1);

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7), entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_NegativeTimeSpan_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 2);

        Assert.Equal(new TimeSpan(-1, -2, -3, -4, -5, -6), entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_ZeroTimeSpan_ReturnsZero()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 3);

        Assert.Equal(TimeSpan.Zero, entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_HoursOnly_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 4);

        Assert.Equal(TimeSpan.FromHours(5), entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_SecondsOnly_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 5);

        Assert.Equal(TimeSpan.FromSeconds(90), entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_LargeTimeSpan_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity entity = db.Table<TestEntity>().First(a => a.Id == 6);

        Assert.Equal(new TimeSpan(365, 0, 0, 0), entity.Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_ViaQuery_ReturnsCorrectTimeSpan()
    {
        using TestDatabase db = SetupDatabase();

        List<TestEntity> results = db.Query<TestEntity>("SELECT * FROM TestEntity WHERE Id = @id",
            new
            {
                id = 1
            });

        Assert.Single(results);
        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7), results[0].Time);
    }

    [Fact]
    public void Read_WhenStoredAsTextCFormat_AllRows_ReturnCorrectCount()
    {
        using TestDatabase db = SetupDatabase();

        List<TestEntity> results = db.Table<TestEntity>().ToList();

        Assert.Equal(6, results.Count);
    }

    [Fact]
    public void Select_Days_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        int days = db.Table<TestEntity>().Select(a => a.Time.Days).First();

        Assert.Equal(2, days);
    }

    [Fact]
    public void Select_Hours_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        int hours = db.Table<TestEntity>().Select(a => a.Time.Hours).First();

        Assert.Equal(3, hours);
    }

    [Fact]
    public void Select_Minutes_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        int minutes = db.Table<TestEntity>().Select(a => a.Time.Minutes).First();

        Assert.Equal(4, minutes);
    }

    [Fact]
    public void Select_TotalDays_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        double totalDays = db.Table<TestEntity>().Select(a => a.Time.TotalDays).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalDays, totalDays, 10);
    }

    [Fact]
    public void Select_TotalHours_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        double totalHours = db.Table<TestEntity>().Select(a => a.Time.TotalHours).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalHours, totalHours, 10);
    }

    [Fact]
    public void Select_TotalSeconds_WhenStoredAsText_ComputesClientSide()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        double totalSeconds = db.Table<TestEntity>().Select(a => a.Time.TotalSeconds).First();

        Assert.Equal(new TimeSpan(2, 3, 4, 5, 6, 7).TotalSeconds, totalSeconds, 5);
    }

    [Fact]
    public void Where_Days_WhenStoredAsText_ThrowsNotSupported()
    {
        using TestDatabase db = SetupTextStorageDatabase();

        Assert.Throws<NotSupportedException>(() => db.Table<TestEntity>().Where(a => a.Time.Days == 2).ToList());
    }

    private static TestDatabase SetupTextStorageDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TimeSpanStorage = TimeSpanStorageMode.Text;
            configure?.Invoke(b);
        }, methodName);
        db.Table<TestEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (1, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = new TimeSpan(2, 3, 4, 5, 6, 7).ToString("c")
            });
        return db;
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<TestEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (1, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = new TimeSpan(2, 3, 4, 5, 6, 7).ToString("c")
            });
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (2, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = new TimeSpan(-1, -2, -3, -4, -5, -6).ToString("c")
            });
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (3, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = TimeSpan.Zero.ToString("c")
            });
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (4, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = TimeSpan.FromHours(5).ToString("c")
            });
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (5, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = TimeSpan.FromSeconds(90).ToString("c")
            });
        db.Execute("INSERT INTO TestEntity (Id, Time) VALUES (6, @time)",
            new SQLiteParameter
            {
                Name = "@time",
                Value = new TimeSpan(365, 0, 0, 0).ToString("c")
            });
        return db;
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required TimeSpan Time { get; set; }
    }
}