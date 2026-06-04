using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DtChainRow
{
    [Key]
    public int Id { get; set; }
    public int Marker { get; set; }
    public DateTime Moment { get; set; }
    public DateTime? NullableMoment { get; set; }
    public DateTimeOffset Offset { get; set; }
}

public class DateTimeComputedPropertyChainTests
{
    private static readonly DtChainRow[] Seed =
    [
        new DtChainRow
        {
            Id = 1, Marker = 11,
            Moment = new DateTime(2021, 5, 10, 14, 30, 45),
            NullableMoment = new DateTime(2021, 5, 10, 14, 30, 45),
            Offset = new DateTimeOffset(2019, 3, 7, 9, 15, 0, TimeSpan.Zero),
        },
        new DtChainRow
        {
            Id = 2, Marker = 22,
            Moment = new DateTime(2020, 1, 15, 8, 0, 0),
            NullableMoment = null,
            Offset = new DateTimeOffset(2018, 12, 31, 23, 0, 0, TimeSpan.Zero),
        },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<DtChainRow>().Schema.CreateTable();
        db.Table<DtChainRow>().AddRange(Seed);
        return db;
    }

    private static List<T> Project<T>(TestDatabase db, System.Linq.Expressions.Expression<Func<DtChainRow, T>> selector)
        => db.Table<DtChainRow>().OrderBy(x => x.Id).Select(selector).ToList();

    [Fact]
    public void DateChain_DateComponents_MatchLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        Assert.Equal([2021, 2020], Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.Year).ToList());
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.Year).ToList(), Project(db, x => x.Moment.Date.Year));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.Month).ToList(), Project(db, x => x.Moment.Date.Month));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.Day).ToList(), Project(db, x => x.Moment.Date.Day));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.DayOfYear).ToList(), Project(db, x => x.Moment.Date.DayOfYear));
    }

    [Fact]
    public void DateChain_TimeComponentsAreZero_MatchLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        Assert.Equal([0, 0], Project(db, x => x.Moment.Date.Hour));
        Assert.Equal([0, 0], Project(db, x => x.Moment.Date.Minute));
        Assert.Equal([0, 0], Project(db, x => x.Moment.Date.Second));
    }

    [Fact]
    public void DateChain_DayOfWeek_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<DayOfWeek> expected = Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date.DayOfWeek).ToList();
        List<DayOfWeek> actual = Project(db, x => x.Moment.Date.DayOfWeek);

        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeOfDayChain_Components_MatchLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        Assert.Equal([14, 8], Project(db, x => x.Moment.TimeOfDay.Hours));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.TimeOfDay.Hours).ToList(), Project(db, x => x.Moment.TimeOfDay.Hours));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.TimeOfDay.Minutes).ToList(), Project(db, x => x.Moment.TimeOfDay.Minutes));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.TimeOfDay.Seconds).ToList(), Project(db, x => x.Moment.TimeOfDay.Seconds));
    }

    [Fact]
    public void Where_DateDay_FiltersOnRealColumn()
    {
        using TestDatabase db = CreateDb();

        List<int> day10 = db.Table<DtChainRow>().Where(x => x.Moment.Date.Day == 10).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> day11 = db.Table<DtChainRow>().Where(x => x.Moment.Date.Day == 11).Select(x => x.Id).ToList();

        Assert.Equal([1], day10);
        Assert.Empty(day11);
    }

    [Fact]
    public void Where_TimeOfDayHours_FiltersOnRealColumn()
    {
        using TestDatabase db = CreateDb();

        List<int> ids = db.Table<DtChainRow>().Where(x => x.Moment.TimeOfDay.Hours == 14).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void DirectDateAndTimeOfDay_StillWork()
    {
        using TestDatabase db = CreateDb();

        List<DateTime> dates = Project(db, x => x.Moment.Date);
        List<TimeSpan> times = Project(db, x => x.Moment.TimeOfDay);

        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.Date).ToList(), dates);
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Moment.TimeOfDay).ToList(), times);
    }

    [Fact]
    public void DateTimeOffsetChain_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        Assert.Equal([2019, 2018], Project(db, x => x.Offset.Date.Year));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Offset.Date.Year).ToList(), Project(db, x => x.Offset.Date.Year));
        Assert.Equal([9, 23], Project(db, x => x.Offset.TimeOfDay.Hours));
        Assert.Equal(Seed.OrderBy(x => x.Id).Select(x => x.Offset.TimeOfDay.Hours).ToList(), Project(db, x => x.Offset.TimeOfDay.Hours));
    }

    [Fact]
    public void NullableDateTimeChain_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(x => x.NullableMoment != null)
            .OrderBy(x => x.Id).Select(x => x.NullableMoment!.Value.Date.Year).ToList();
        List<int> actual = db.Table<DtChainRow>().Where(x => x.NullableMoment != null)
            .OrderBy(x => x.Id).Select(x => x.NullableMoment!.Value.Date.Year).ToList();

        Assert.Equal([2021], expected);
        Assert.Equal(expected, actual);
    }
}
