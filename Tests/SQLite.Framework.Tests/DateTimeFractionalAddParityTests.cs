using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DtEdgeRows")]
public class DtEdgeRow
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Moment")]
    public DateTime Moment { get; set; }
}

public class DateTimeFractionalAddParityTests
{
    private static (TestDatabase Db, List<DtEdgeRow> Mem) Seed(DateTime moment, Action<SQLiteOptionsBuilder>? cfg = null)
    {
        TestDatabase db = cfg == null ? new() : new(cfg);
        db.Table<DtEdgeRow>().Schema.CreateTable();
        DtEdgeRow row = new() { Id = 1, Moment = moment };
        db.Table<DtEdgeRow>().Add(row);
        List<DtEdgeRow> mem = new() { new DtEdgeRow { Id = 1, Moment = moment } };
        return (db, mem);
    }

    [Fact]
    public void AddSecondsFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddSeconds(12.3456789).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddSeconds(12.3456789).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddHoursFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddHours(12.3456789).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddHours(12.3456789).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddMinutesFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddMinutes(7.7777777).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddMinutes(7.7777777).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddDaysFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddDays(99.9999).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddDays(99.9999).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddMillisecondsFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddMilliseconds(99.9999).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddMilliseconds(99.9999).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddSecondsNegativeFractionalWithinOneTick()
    {
        var (db, mem) = Seed(new DateTime(2001, 5, 6, 7, 8, 9));
        var expected = mem.Select(r => r.Moment.AddSeconds(-12.3456789).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddSeconds(-12.3456789).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }

    [Fact]
    public void AddHoursColumnArgWithinOneTick()
    {
        TestDatabase db = new();
        db.Table<DtEdgeRow>().Schema.CreateTable();
        DateTime moment = new(2001, 5, 6, 7, 8, 9);
        db.Table<DtEdgeRow>().Add(new DtEdgeRow { Id = 12, Moment = moment });
        List<DtEdgeRow> mem = new() { new DtEdgeRow { Id = 12, Moment = moment } };
        var expected = mem.Select(r => r.Moment.AddSeconds(r.Id + 0.345678).Ticks).First();
        var actual = db.Table<DtEdgeRow>().Select(r => r.Moment.AddSeconds(r.Id + 0.345678).Ticks).First();
        db.Dispose();
        Assert.True(Math.Abs(actual - expected) <= 1);
    }
}
