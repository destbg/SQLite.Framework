using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DowTextGroup")]
public class DowTextGroupRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class DayOfWeekTextStorageGroupKeyTests
{
    [Fact]
    public void GroupByComputedDayOfWeekKeyFilterTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowTextGroupRow>().Schema.CreateTable();
        List<DowTextGroupRow> mem =
        [
            new() { Id = 1, When = new DateTime(2024, 1, 1) },
            new() { Id = 2, When = new DateTime(2024, 1, 2) },
            new() { Id = 3, When = new DateTime(2024, 1, 8) },
        ];
        foreach (DowTextGroupRow row in mem)
        {
            db.Table<DowTextGroupRow>().Add(row);
        }

        List<int> expected = mem.GroupBy(r => r.When.DayOfWeek).Where(g => g.Key == DayOfWeek.Monday).Select(g => g.Count()).ToList();
        List<int> actual = db.Table<DowTextGroupRow>().GroupBy(r => r.When.DayOfWeek).Where(g => g.Key == DayOfWeek.Monday).Select(g => g.Count()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedComputedDayOfWeekComparedToConstantTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowTextGroupRow>().Schema.CreateTable();
        List<DowTextGroupRow> mem =
        [
            new() { Id = 1, When = new DateTime(2024, 1, 1) },
            new() { Id = 2, When = new DateTime(2024, 1, 2) },
        ];
        foreach (DowTextGroupRow row in mem)
        {
            db.Table<DowTextGroupRow>().Add(row);
        }

        List<DayOfWeek> expected = mem.Select(r => r.When.DayOfWeek).Where(d => d == DayOfWeek.Monday).ToList();
        List<DayOfWeek> actual = db.Table<DowTextGroupRow>().Select(r => r.When.DayOfWeek).Where(d => d == DayOfWeek.Monday).ToList();

        Assert.Equal(expected, actual);
    }
}
