using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableDowRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek? MaybeDow { get; set; }
}

public class NullableDayOfWeekComputedComparisonTests
{
    private static List<NullableDowRow> Rows() =>
    [
        new() { Id = 1, When = new DateTime(2024, 1, 1), MaybeDow = DayOfWeek.Monday },
        new() { Id = 2, When = new DateTime(2024, 1, 2), MaybeDow = DayOfWeek.Wednesday },
        new() { Id = 3, When = new DateTime(2024, 1, 8), MaybeDow = null },
    ];

    private static TestDatabase Seed(EnumStorageMode storage)
    {
        TestDatabase db = new(b => b.EnumStorage = storage);
        db.Table<NullableDowRow>().Schema.CreateTable();
        db.Table<NullableDowRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void NullableColumnEqualsComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Rows().Where(r => r.MaybeDow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<NullableDowRow>()
            .Where(r => r.MaybeDow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedDayOfWeekEqualsNullableColumnTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Rows().Where(r => r.When.DayOfWeek == r.MaybeDow).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<NullableDowRow>()
            .Where(r => r.When.DayOfWeek == r.MaybeDow).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableColumnEqualsComputedDayOfWeekIntegerStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Integer);

        List<int> expected = Rows().Where(r => r.MaybeDow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<NullableDowRow>()
            .Where(r => r.MaybeDow == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCapturedValueEqualsComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);
        DayOfWeek? none = null;

        List<int> expected = Rows().Where(r => none == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([], expected);

        List<int> actual = db.Table<NullableDowRow>()
            .Where(r => none == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableColumnNotEqualsComputedDayOfWeekTextStorage()
    {
        using TestDatabase db = Seed(EnumStorageMode.Text);

        List<int> expected = Rows().Where(r => r.MaybeDow != r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2, 3], expected);

        List<int> actual = db.Table<NullableDowRow>()
            .Where(r => r.MaybeDow != r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
