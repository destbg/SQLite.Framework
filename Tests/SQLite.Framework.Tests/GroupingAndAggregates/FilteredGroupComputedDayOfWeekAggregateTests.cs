using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26dFilteredDayBuckets")]
public class H26dFilteredDayBucket
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }

    public int Amount { get; set; }

    public DateTime When { get; set; }
}

public class FilteredGroupComputedDayOfWeekAggregateTests
{
    [Fact]
    public void ComparingTheFilteredMaxComputedDayOfWeekKeepsTheLinqValues()
    {
        using TestDatabase db = Setup(nameof(ComparingTheFilteredMaxComputedDayOfWeekKeepsTheLinqValues));

        List<bool> expected = Rows()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Where(x => x.Amount > 0).Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<H26dFilteredDayBucket>()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Where(x => x.Amount > 0).Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComparingTheFilteredMinComputedDayOfWeekKeepsTheLinqValues()
    {
        using TestDatabase db = Setup(nameof(ComparingTheFilteredMinComputedDayOfWeekKeepsTheLinqValues));

        List<bool> expected = Rows()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Where(x => x.Amount > 0).Min(x => x.When.DayOfWeek) == DayOfWeek.Sunday)
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<H26dFilteredDayBucket>()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Where(x => x.Amount > 0).Min(x => x.When.DayOfWeek) == DayOfWeek.Sunday)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteringGroupsByTheFilteredMaxComputedDayOfWeekKeepsTheLinqGroups()
    {
        using TestDatabase db = Setup(nameof(FilteringGroupsByTheFilteredMaxComputedDayOfWeekKeepsTheLinqGroups));

        List<int> expected = Rows()
            .GroupBy(r => r.Bucket)
            .Where(g => g.Where(x => x.Amount > 0).Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<H26dFilteredDayBucket>()
            .GroupBy(r => r.Bucket)
            .Where(g => g.Where(x => x.Amount > 0).Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26dFilteredDayBucket> Rows()
    {
        return
        [
            new H26dFilteredDayBucket { Id = 1, Bucket = 1, Amount = 5, When = new DateTime(2024, 1, 7, 9, 0, 0) },
            new H26dFilteredDayBucket { Id = 2, Bucket = 1, Amount = 5, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H26dFilteredDayBucket { Id = 3, Bucket = 1, Amount = 0, When = new DateTime(2024, 1, 6, 9, 0, 0) },
            new H26dFilteredDayBucket { Id = 4, Bucket = 2, Amount = 5, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H26dFilteredDayBucket { Id = 5, Bucket = 2, Amount = 5, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<H26dFilteredDayBucket>().Schema.CreateTable();
        db.Table<H26dFilteredDayBucket>().AddRange(Rows());
        return db;
    }
}
