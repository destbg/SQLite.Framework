using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DtoComponentRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset When { get; set; }
}

public class DateTimeOffsetComponentUtcTicksParityTests
{
    [Fact]
    public void HourOfDateTimeOffset_UnderUtcTicksStorage_ReadsBackInUtc()
    {
        using TestDatabase db = new(b => b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks);
        db.Table<DtoComponentRow>().Schema.CreateTable();

        DateTimeOffset when = new(2020, 1, 1, 10, 0, 0, TimeSpan.FromHours(5));
        db.Table<DtoComponentRow>().Add(new DtoComponentRow { Id = 1, When = when });

        List<int> actual = db.Table<DtoComponentRow>().Select(r => r.When.Hour).ToList();

        Assert.Equal(new List<int> { when.UtcDateTime.Hour }, actual);
    }
}
