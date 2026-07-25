using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DayOfWeekEnumTextStorageComparisonTests
{
    internal sealed class DayOfWeekTextRow
    {
        [Key]
        public int Id { get; set; }

        public DateTime When { get; set; }

        public DateTimeOffset WhenOffset { get; set; }

        public DateOnly WhenDate { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
    }

    private static List<DayOfWeekTextRow> Rows() =>
    [
        new() { Id = 1, When = new DateTime(2024, 1, 1), WhenOffset = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), WhenDate = new DateOnly(2024, 1, 1), DayOfWeek = DayOfWeek.Monday },
        new() { Id = 2, When = new DateTime(2024, 1, 2), WhenOffset = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), WhenDate = new DateOnly(2024, 1, 2), DayOfWeek = DayOfWeek.Tuesday },
        new() { Id = 3, When = new DateTime(2024, 1, 8), WhenOffset = new DateTimeOffset(2024, 1, 8, 0, 0, 0, TimeSpan.Zero), WhenDate = new DateOnly(2024, 1, 8), DayOfWeek = DayOfWeek.Monday },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<DayOfWeekTextRow>().Schema.CreateTable();
        db.Table<DayOfWeekTextRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void DateTimeDayOfWeekEqualsConstant()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.When.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DayOfWeekTextRow>().Where(r => r.When.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantEqualsDateTimeDayOfWeek()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => DayOfWeek.Monday == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();

        List<int> actual = db.Table<DayOfWeekTextRow>().Where(r => DayOfWeek.Monday == r.When.DayOfWeek).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeOffsetDayOfWeekEqualsConstant()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.WhenOffset.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();

        List<int> actual = db.Table<DayOfWeekTextRow>().Where(r => r.WhenOffset.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateOnlyDayOfWeekEqualsConstant()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.WhenDate.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();

        List<int> actual = db.Table<DayOfWeekTextRow>().Where(r => r.WhenDate.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StoredDayOfWeekColumnKeepsEnumStorage()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => r.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<DayOfWeekTextRow>().Where(r => r.DayOfWeek == DayOfWeek.Monday).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
