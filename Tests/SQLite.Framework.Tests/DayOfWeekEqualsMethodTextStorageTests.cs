using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DowEqualsRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class DayOfWeekEqualsMethodTextStorageTests
{
    private static List<DowEqualsRow> Rows() =>
    [
        new() { Id = 1, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
        new() { Id = 2, When = new DateTime(2024, 1, 2), Dow = DayOfWeek.Wednesday },
        new() { Id = 3, When = new DateTime(2024, 1, 8), Dow = DayOfWeek.Monday },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<DowEqualsRow>().Schema.CreateTable();
        db.Table<DowEqualsRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ComputedDayOfWeekEqualsMethodWithConstant()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.When.DayOfWeek.Equals(DayOfWeek.Monday)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DowEqualsRow>()
            .Where(r => r.When.DayOfWeek.Equals(DayOfWeek.Monday)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StoredColumnEqualsMethodWithComputedDayOfWeek()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.Dow.Equals(r.When.DayOfWeek)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DowEqualsRow>()
            .Where(r => r.Dow.Equals(r.When.DayOfWeek)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedDayOfWeekEqualsMethodWithStoredColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.When.DayOfWeek.Equals(r.Dow)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DowEqualsRow>()
            .Where(r => r.When.DayOfWeek.Equals(r.Dow)).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StoredColumnRelationalAgainstComputedDayOfWeek()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.Dow > r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<DowEqualsRow>()
            .Where(r => r.Dow > r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
