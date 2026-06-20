using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TimeOnlyAddRow
{
    [Key]
    public int Id { get; set; }

    public TimeOnly Time { get; set; }
}

public class TimeOnlyAddTimeSpanTextStorageTests
{
    [Fact]
    public void AddTimeSpan_TextTimeSpanStorage_MatchesObjects()
    {
        using TestDatabase db = Setup(TimeSpanStorageMode.Text);
        TimeSpan span = TimeSpan.FromHours(1);

        TimeOnly expected = db.Table<TimeOnlyAddRow>().AsEnumerable()
            .First(r => r.Id == 1).Time.Add(span);

        TimeOnly actual = db.Table<TimeOnlyAddRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Time.Add(span))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddTimeSpan_IntegerTimeSpanStorage_MatchesObjects()
    {
        using TestDatabase db = Setup(TimeSpanStorageMode.Integer);
        TimeSpan span = TimeSpan.FromHours(1);

        TimeOnly expected = db.Table<TimeOnlyAddRow>().AsEnumerable()
            .First(r => r.Id == 1).Time.Add(span);

        TimeOnly actual = db.Table<TimeOnlyAddRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Time.Add(span))
            .First();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Setup(TimeSpanStorageMode mode)
    {
        TestDatabase db = new(b => b.UseTimeSpanStorage(mode));
        db.Table<TimeOnlyAddRow>().Schema.CreateTable();
        db.Table<TimeOnlyAddRow>().Add(new TimeOnlyAddRow { Id = 1, Time = new TimeOnly(3, 4, 5) });
        return db;
    }
}
