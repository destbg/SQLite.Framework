using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DowScheduleRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class DowSlotRow
{
    [Key]
    public int Id { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class ComputedDayOfWeekJoinAndContainsTests
{
    private static List<DowScheduleRow> Schedules() =>
    [
        new() { Id = 1, When = new DateTime(2024, 1, 1) },
        new() { Id = 2, When = new DateTime(2024, 1, 2) },
        new() { Id = 3, When = new DateTime(2024, 1, 8) },
    ];

    private static List<DowSlotRow> Slots() =>
    [
        new() { Id = 10, Dow = DayOfWeek.Monday },
        new() { Id = 20, Dow = DayOfWeek.Friday },
    ];

    private static TestDatabase Seed(EnumStorageMode storage)
    {
        TestDatabase db = new(b => b.EnumStorage = storage);
        db.Table<DowScheduleRow>().Schema.CreateTable();
        db.Table<DowSlotRow>().Schema.CreateTable();
        db.Table<DowScheduleRow>().AddRange(Schedules());
        db.Table<DowSlotRow>().AddRange(Slots());
        return db;
    }

    [Fact]
    public void JoinOnComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Schedules()
            .Join(Slots(), s => s.When.DayOfWeek, t => t.Dow, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal([110, 310], expected);

        List<int> actual = db.Table<DowScheduleRow>()
            .Join(db.Table<DowSlotRow>(), s => s.When.DayOfWeek, t => t.Dow, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnComputedDayOfWeekIntegerStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Integer);

        List<int> expected = Schedules()
            .Join(Slots(), s => s.When.DayOfWeek, t => t.Dow, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal([110, 310], expected);

        List<int> actual = db.Table<DowScheduleRow>()
            .Join(db.Table<DowSlotRow>(), s => s.When.DayOfWeek, t => t.Dow, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnComputedDayOfWeekCompositeKeyTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Schedules()
            .Join(Slots(), s => new { D = s.When.DayOfWeek, K = 1 }, t => new { D = t.Dow, K = 1 }, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal([110, 310], expected);

        List<int> actual = db.Table<DowScheduleRow>()
            .Join(db.Table<DowSlotRow>(), s => new { D = s.When.DayOfWeek, K = 1 }, t => new { D = t.Dow, K = 1 }, (s, t) => s.Id * 100 + t.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnComputedDayOfWeekCompositeKeyInnerSideTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Slots()
            .Join(Schedules(), t => new { D = t.Dow, K = 1 }, s => new { D = s.When.DayOfWeek, K = 1 }, (t, s) => t.Id * 100 + s.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal([1001, 1003], expected);

        List<int> actual = db.Table<DowSlotRow>()
            .Join(db.Table<DowScheduleRow>(), t => new { D = t.Dow, K = 1 }, s => new { D = s.When.DayOfWeek, K = 1 }, (t, s) => t.Id * 100 + s.Id)
            .OrderBy(v => v).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayContainsNullableComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        DayOfWeek?[] days = [DayOfWeek.Monday, null];

        List<int> expected = Schedules().Where(s => days.Contains(s.When.DayOfWeek)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DowScheduleRow>().Where(s => days.Contains(s.When.DayOfWeek)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayContainsComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        DayOfWeek[] days = [DayOfWeek.Monday, DayOfWeek.Friday];

        List<int> expected = Schedules().Where(s => days.Contains(s.When.DayOfWeek)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DowScheduleRow>().Where(s => days.Contains(s.When.DayOfWeek)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayContainsStoredDayOfWeekColumnTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        DayOfWeek[] days = [DayOfWeek.Monday, DayOfWeek.Friday];

        List<int> expected = Slots().Where(s => days.Contains(s.Dow)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal([10, 20], expected);

        List<int> actual = db.Table<DowSlotRow>().Where(s => days.Contains(s.Dow)).Select(s => s.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParameterizedSubqueryContainsStoredDayOfWeekColumnTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        int minId = 1;

        List<int> expected = Slots()
            .Where(t => Schedules().Where(s => s.Id > minId).Select(s => s.When.DayOfWeek).Contains(t.Dow))
            .Select(t => t.Id).OrderBy(id => id).ToList();
        Assert.Equal([10], expected);

        List<int> actual = db.Table<DowSlotRow>()
            .Where(t => db.Table<DowScheduleRow>().Where(s => s.Id > minId).Select(s => s.When.DayOfWeek).Contains(t.Dow))
            .Select(t => t.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
