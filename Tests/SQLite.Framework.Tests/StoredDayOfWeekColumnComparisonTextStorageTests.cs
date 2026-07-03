using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class StoredDayOfWeekRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class StoredDayOfWeekColumnComparisonTextStorageTests
{
    private static List<StoredDayOfWeekRow> Rows() =>
    [
        new() { Id = 1, When = new DateTime(2024, 1, 1), Dow = DayOfWeek.Monday },
        new() { Id = 2, When = new DateTime(2024, 1, 2), Dow = DayOfWeek.Wednesday },
        new() { Id = 3, When = new DateTime(2024, 1, 8), Dow = DayOfWeek.Monday },
    ];

    private static TestDatabase Seed(EnumStorageMode storage = EnumStorageMode.Text)
    {
        TestDatabase db = new(b => b.EnumStorage = storage);
        db.Table<StoredDayOfWeekRow>().Schema.CreateTable();
        db.Table<StoredDayOfWeekRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ColumnEqualsComputedDayOfWeek()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.Dow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<StoredDayOfWeekRow>()
            .Where(r => r.Dow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedDayOfWeekEqualsColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.When.DayOfWeek == r.Dow).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<StoredDayOfWeekRow>()
            .Where(r => r.When.DayOfWeek == r.Dow).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnEqualsComputedDayOfWeekUnderIntegerStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Integer);

        List<int> expected = Rows().Where(r => r.Dow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<StoredDayOfWeekRow>()
            .Where(r => r.Dow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
