using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TextFormattedDateTimeRow
{
    [Key] public int Id { get; set; }
    public DateTime Ts { get; set; }
}

public class TextFormattedDateTimeOffsetRow
{
    [Key] public int Id { get; set; }
    public DateTimeOffset Dto { get; set; }
}

public class TextFormattedTimeOnlyRow
{
    [Key] public int Id { get; set; }
    public TimeOnly T { get; set; }
}

public class TextFormattedSubSecondStorageParityTests
{
    [Fact]
    public void DateTime_TextFormatted_RoundTripsSubSecond()
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted));
        db.Table<TextFormattedDateTimeRow>().Schema.CreateTable();
        DateTime value = new(2024, 6, 1, 10, 30, 0, 500);
        db.Table<TextFormattedDateTimeRow>().Add(new TextFormattedDateTimeRow { Id = 1, Ts = value });
        DateTime back = db.Table<TextFormattedDateTimeRow>().Single(e => e.Id == 1).Ts;
        Assert.Equal(value, back);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_RoundTripsSubSecond()
    {
        using TestDatabase db = new(b => b.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.TextFormatted));
        db.Table<TextFormattedDateTimeOffsetRow>().Schema.CreateTable();
        DateTimeOffset value = new(2024, 6, 1, 10, 30, 0, 500, TimeSpan.FromHours(5));
        db.Table<TextFormattedDateTimeOffsetRow>().Add(new TextFormattedDateTimeOffsetRow { Id = 1, Dto = value });
        DateTimeOffset back = db.Table<TextFormattedDateTimeOffsetRow>().Single(e => e.Id == 1).Dto;
        Assert.Equal(value, back);
    }

    [Fact]
    public void TimeOnly_Text_RoundTripsSubSecond()
    {
        using TestDatabase db = new(b => b.UseTimeOnlyStorage(TimeOnlyStorageMode.Text));
        db.Table<TextFormattedTimeOnlyRow>().Schema.CreateTable();
        TimeOnly value = new(10, 30, 0, 500);
        db.Table<TextFormattedTimeOnlyRow>().Add(new TextFormattedTimeOnlyRow { Id = 1, T = value });
        TimeOnly back = db.Table<TextFormattedTimeOnlyRow>().Single(e => e.Id == 1).T;
        Assert.Equal(value, back);
    }
}
