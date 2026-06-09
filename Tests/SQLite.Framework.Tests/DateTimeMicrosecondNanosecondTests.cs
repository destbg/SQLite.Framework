using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class DateTimePrecisionRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public DateTimeOffset Dto { get; set; }
}

public class DateTimeMicrosecondNanosecondTests
{
    private static DateTime SampleDateTime => new DateTime(2020, 5, 15, 3, 4, 5).AddTicks(67_895);

    private static DateTimeOffset SampleDateTimeOffset => new DateTimeOffset(2020, 5, 15, 3, 4, 5, TimeSpan.Zero).AddTicks(67_895);

    [Fact]
    public void DateTime_Microsecond_InSelectProjection_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTime value = SampleDateTime;
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = value });

        int oracle = value.Microsecond;

        int actual = db.Table<DateTimePrecisionRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Dt.Microsecond)
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTime_Nanosecond_InSelectProjection_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTime value = SampleDateTime;
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = value });

        int oracle = value.Nanosecond;

        int actual = db.Table<DateTimePrecisionRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Dt.Nanosecond)
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTime_Microsecond_InWhereClause_FiltersLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTime matching = new DateTime(2020, 5, 15, 3, 4, 5).AddTicks(67_895);
        DateTime other = new DateTime(2020, 5, 15, 3, 4, 5).AddTicks(12_345);
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = matching });
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 2, Dt = other });

        int micro = matching.Microsecond;
        List<(int, DateTime)> seed = [(1, matching), (2, other)];
        List<int> oracle = seed.Where(r => r.Item2.Microsecond == micro).Select(r => r.Item1).ToList();

        List<int> actual = db.Table<DateTimePrecisionRow>()
            .Where(x => x.Dt.Microsecond == micro)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTime_Nanosecond_InOrderByClause_OrdersLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTime a = new DateTime(2020, 5, 15, 3, 4, 5).AddTicks(9);
        DateTime b = new DateTime(2020, 5, 15, 3, 4, 5).AddTicks(3);
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = a });
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 2, Dt = b });

        List<(int, DateTime)> seed = [(1, a), (2, b)];
        List<int> oracle = seed.OrderBy(r => r.Item2.Nanosecond).Select(r => r.Item1).ToList();

        List<int> actual = db.Table<DateTimePrecisionRow>()
            .OrderBy(x => x.Dt.Nanosecond)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeOffset_Microsecond_InSelectProjection_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTimeOffset value = SampleDateTimeOffset;
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = DateTime.UnixEpoch, Dto = value });

        int oracle = value.Microsecond;

        int actual = db.Table<DateTimePrecisionRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Dto.Microsecond)
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeOffset_Nanosecond_InWhereClause_FiltersLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<DateTimePrecisionRow>().Schema.CreateTable();
        DateTimeOffset matching = new DateTimeOffset(2020, 5, 15, 3, 4, 5, TimeSpan.Zero).AddTicks(9);
        DateTimeOffset other = new DateTimeOffset(2020, 5, 15, 3, 4, 5, TimeSpan.Zero).AddTicks(3);
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 1, Dt = DateTime.UnixEpoch, Dto = matching });
        db.Table<DateTimePrecisionRow>().Add(new DateTimePrecisionRow { Id = 2, Dt = DateTime.UnixEpoch, Dto = other });

        int nano = matching.Nanosecond;
        List<(int, DateTimeOffset)> seed = [(1, matching), (2, other)];
        List<int> oracle = seed.Where(r => r.Item2.Nanosecond == nano).Select(r => r.Item1).ToList();

        List<int> actual = db.Table<DateTimePrecisionRow>()
            .Where(x => x.Dto.Nanosecond == nano)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
