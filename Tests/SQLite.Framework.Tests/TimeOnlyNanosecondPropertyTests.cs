using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class NanosecondTimeRow
{
    [Key]
    public int Id { get; set; }

    public TimeOnly Time { get; set; }
}

public class TimeOnlyNanosecondPropertyTests
{
    [Fact]
    public void Nanosecond_NonZeroValue_ReturnsNanosecondComponent()
    {
        using TestDatabase db = new();
        db.Table<NanosecondTimeRow>().Schema.CreateTable();

        long ticks = TimeSpan.TicksPerHour * 3 + TimeSpan.TicksPerMicrosecond * 7 + 5;
        TimeOnly time = new(ticks);
        db.Table<NanosecondTimeRow>().Add(new NanosecondTimeRow { Id = 1, Time = time });

        int oracle = time.Nanosecond;

        int actual = db.Table<NanosecondTimeRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Time.Nanosecond)
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Nanosecond_ZeroValue_ReturnsZero()
    {
        using TestDatabase db = new();
        db.Table<NanosecondTimeRow>().Schema.CreateTable();

        TimeOnly time = new(3, 4, 5, 6, 7);
        db.Table<NanosecondTimeRow>().Add(new NanosecondTimeRow { Id = 1, Time = time });

        int oracle = time.Nanosecond;
        Assert.Equal(0, oracle);

        int actual = db.Table<NanosecondTimeRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Time.Nanosecond)
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Nanosecond_UsedInWhereFilter_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<NanosecondTimeRow>().Schema.CreateTable();

        long ticksA = TimeSpan.TicksPerHour + 5;
        long ticksB = TimeSpan.TicksPerHour + 3;
        TimeOnly timeA = new(ticksA);
        TimeOnly timeB = new(ticksB);

        db.Table<NanosecondTimeRow>().Add(new NanosecondTimeRow { Id = 1, Time = timeA });
        db.Table<NanosecondTimeRow>().Add(new NanosecondTimeRow { Id = 2, Time = timeB });

        int nsA = timeA.Nanosecond;

        List<int> oracle = new List<(int, TimeOnly)> { (1, timeA), (2, timeB) }
            .Where(r => r.Item2.Nanosecond == nsA)
            .Select(r => r.Item1)
            .ToList();

        List<int> actual = db.Table<NanosecondTimeRow>()
            .Where(x => x.Time.Nanosecond == nsA)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
