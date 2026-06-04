using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class DateTimeComputedPropertyChainBugTests
{
    [Fact]
    public void DateDotYearChainReturnsRealYear()
    {
        DateTime moment = new DateTime(2021, 5, 10, 14, 30, 45);
        List<datetimeChainRow> seed = new() { new datetimeChainRow { Id = 1, Marker = 11, Moment = moment } };
        using TestDatabase db = new();
        db.Table<datetimeChainRow>().Schema.CreateTable();
        db.Table<datetimeChainRow>().AddRange(seed);
        List<int> expected = seed.Select(x => x.Moment.Date.Year).ToList();
        List<int> actual = db.Table<datetimeChainRow>().Select(x => x.Moment.Date.Year).ToList();
        Assert.Equal(expected, actual);
    }
}

public class datetimeChainRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int Marker { get; set; }
    public DateTime Moment { get; set; }
}
