using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DtPlusTsColumnRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public TimeSpan Duration { get; set; }
}

public class DateTimeAddTimeSpanColumnTextStorageParityTests
{
    [Fact]
    public void DateTimePlusTimeSpanColumn_UnderTextTimeSpanStorage_DoesNotAddTheDuration()
    {
        using TestDatabase db = new(b => b.TimeSpanStorage = TimeSpanStorageMode.Text);
        db.Table<DtPlusTsColumnRow>().Schema.CreateTable();

        DateTime when = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        db.Table<DtPlusTsColumnRow>().Add(new DtPlusTsColumnRow { Id = 1, When = when, Duration = TimeSpan.FromDays(2) });

        Assert.ThrowsAny<Exception>(() => db.Table<DtPlusTsColumnRow>().Select(r => r.When + r.Duration).ToList());
    }
}
