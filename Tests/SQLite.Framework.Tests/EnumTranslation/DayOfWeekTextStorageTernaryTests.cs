using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DowMix")]
public class DowMixRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public DateTime When { get; set; }

    public DateTime Other { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class DayOfWeekTextStorageTernaryTests
{
    [Fact]
    public void TernaryComputedOrStoredDayOfWeekTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowMixRow>().Schema.CreateTable();
        List<DowMixRow> mem =
        [
            new() { Id = 1, Flag = true, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Friday },
            new() { Id = 2, Flag = false, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
        ];
        foreach (DowMixRow row in mem)
        {
            db.Table<DowMixRow>().Add(row);
        }

        List<int> expected = mem.Where(r => (r.Flag ? r.When.DayOfWeek : r.Dow) == DayOfWeek.Monday).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<DowMixRow>().Where(r => (r.Flag ? r.When.DayOfWeek : r.Dow) == DayOfWeek.Monday).Select(r => r.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TernaryStoredOrComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowMixRow>().Schema.CreateTable();
        List<DowMixRow> mem =
        [
            new() { Id = 1, Flag = true, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Friday },
            new() { Id = 2, Flag = false, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
        ];
        foreach (DowMixRow row in mem)
        {
            db.Table<DowMixRow>().Add(row);
        }

        List<int> expected = mem.Where(r => (r.Flag ? r.Dow : r.When.DayOfWeek) == DayOfWeek.Monday).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<DowMixRow>().Where(r => (r.Flag ? r.Dow : r.When.DayOfWeek) == DayOfWeek.Monday).Select(r => r.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TernaryBothComputedDayOfWeekMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<DowMixRow>().Schema.CreateTable();
        List<DowMixRow> mem =
        [
            new() { Id = 1, Flag = true, When = new DateTime(2024, 1, 1), Other = new DateTime(2024, 1, 2) },
            new() { Id = 2, Flag = false, When = new DateTime(2024, 1, 3), Other = new DateTime(2024, 1, 8) },
        ];
        foreach (DowMixRow row in mem)
        {
            db.Table<DowMixRow>().Add(row);
        }

        List<int> expected = mem.Where(r => (r.Flag ? r.When.DayOfWeek : r.Other.DayOfWeek) == DayOfWeek.Monday).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<DowMixRow>().Where(r => (r.Flag ? r.When.DayOfWeek : r.Other.DayOfWeek) == DayOfWeek.Monday).Select(r => r.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntegerStorageTernaryMixedDayOfWeekMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<DowMixRow>().Schema.CreateTable();
        List<DowMixRow> mem =
        [
            new() { Id = 1, Flag = true, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Friday },
            new() { Id = 2, Flag = false, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
        ];
        foreach (DowMixRow row in mem)
        {
            db.Table<DowMixRow>().Add(row);
        }

        List<int> expected = mem.Where(r => (r.Flag ? r.When.DayOfWeek : r.Dow) == DayOfWeek.Monday).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<DowMixRow>().Where(r => (r.Flag ? r.When.DayOfWeek : r.Dow) == DayOfWeek.Monday).Select(r => r.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntegerStorageComputedVsStoredDayOfWeekMatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<DowMixRow>().Schema.CreateTable();
        List<DowMixRow> mem =
        [
            new() { Id = 1, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
            new() { Id = 2, When = new DateTime(2024, 1, 2), Dow = DayOfWeek.Monday },
        ];
        foreach (DowMixRow row in mem)
        {
            db.Table<DowMixRow>().Add(row);
        }

        List<int> expected = mem.Where(r => r.When.DayOfWeek == r.Dow).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<DowMixRow>().Where(r => r.When.DayOfWeek == r.Dow).Select(r => r.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
