using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeOnlyAddNegativeFractionalTests
{
    [Fact]
    public void AddHoursNegativeFractionalMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
        TimeOnly seed = new(12, 0, 0);
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = seed });

        long oracle = new[] { seed }.Select(t => t.AddHours(-1.0 / 7.0)).First().Ticks;
        Assert.Equal(426857142858L, oracle);

        long actual = db.Table<TimeOnlyMethodEntity>()
            .Where(x => x.Id == 1)
            .Select(x => x.Time.AddHours(-1.0 / 7.0))
            .First()
            .Ticks;

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AddMinutesNegativeFractionalMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
        TimeOnly seed = new(12, 0, 0);
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = seed });

        long oracle = new[] { seed }.Select(t => t.AddMinutes(-1.0 / 7.0)).First().Ticks;
        Assert.Equal(431914285715L, oracle);

        long actual = db.Table<TimeOnlyMethodEntity>()
            .Where(x => x.Id == 1)
            .Select(x => x.Time.AddMinutes(-1.0 / 7.0))
            .First()
            .Ticks;

        Assert.Equal(oracle, actual);
    }
}
