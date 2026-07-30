using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25qDowBuckets")]
public class H25qDowBucketRow
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }

    public DateTime When { get; set; }
}

public class GroupedComputedDayOfWeekAggregateComparisonTests
{
    [Fact]
    public void FilteringGroupsByTheMaxComputedDayOfWeekKeepsTheLinqGroups()
    {
        using TestDatabase db = Setup(nameof(FilteringGroupsByTheMaxComputedDayOfWeekKeepsTheLinqGroups));

        List<int> expected = Rows()
            .GroupBy(r => r.Bucket)
            .Where(g => g.Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<H25qDowBucketRow>()
            .GroupBy(r => r.Bucket)
            .Where(g => g.Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectingAComparisonOfTheMaxComputedDayOfWeekKeepsTheLinqValues()
    {
        using TestDatabase db = Setup(nameof(ProjectingAComparisonOfTheMaxComputedDayOfWeekKeepsTheLinqValues));

        List<bool> expected = Rows()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<H25qDowBucketRow>()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Max(x => x.When.DayOfWeek) == DayOfWeek.Monday)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheScalarMaxComputedDayOfWeekKeepsTheLinqValue()
    {
        using TestDatabase db = Setup(nameof(TheScalarMaxComputedDayOfWeekKeepsTheLinqValue));

        DayOfWeek expected = Rows().Max(r => r.When.DayOfWeek);

        Assert.Equal(expected, db.Table<H25qDowBucketRow>().Max(r => r.When.DayOfWeek));
    }

    [Fact]
    public void TheScalarMinComputedDayOfWeekKeepsTheLinqValue()
    {
        using TestDatabase db = Setup(nameof(TheScalarMinComputedDayOfWeekKeepsTheLinqValue));

        DayOfWeek expected = Rows().Min(r => r.When.DayOfWeek);

        Assert.Equal(expected, db.Table<H25qDowBucketRow>().Min(r => r.When.DayOfWeek));
    }

    private static List<H25qDowBucketRow> Rows()
    {
        return
        [
            new H25qDowBucketRow { Id = 1, Bucket = 1, When = new DateTime(2024, 1, 7, 9, 0, 0) },
            new H25qDowBucketRow { Id = 2, Bucket = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H25qDowBucketRow { Id = 3, Bucket = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H25qDowBucketRow { Id = 4, Bucket = 2, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<H25qDowBucketRow>().Schema.CreateTable();
        db.Table<H25qDowBucketRow>().AddRange(Rows());
        return db;
    }
}
