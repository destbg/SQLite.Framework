using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23fGroupAggregateRows")]
public class H23fGroupAggregateRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupedAggregateProjectionFilterTests
{
    [Fact]
    public void FilteringProjectedGroupCountsMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(FilteringProjectedGroupCountsMatchesLinq));

        List<int> expected = Numbers().GroupBy(n => n).Select(g => g.Count()).Where(c => c > 1).OrderBy(c => c).ToList();
        List<int> actual = db.Table<H23fGroupAggregateRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Select(g => g.Count()).Where(c => c > 1))
            .First()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyOverProjectedGroupCountsMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AnyOverProjectedGroupCountsMatchesLinq));

        bool expected = Numbers().GroupBy(n => n).Select(g => g.Count()).Any(c => c > 2);
        bool actual = db.Table<H23fGroupAggregateRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Select(g => g.Count()).Any(c => c > 2))
            .First();

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountingProjectedGroupSumsWithAPredicateMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(CountingProjectedGroupSumsWithAPredicateMatchesLinq));

        int expected = Numbers().GroupBy(n => n).Select(g => g.Sum()).Count(s => s > 8);
        int actual = db.Table<H23fGroupAggregateRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Select(g => g.Sum()).Count(s => s > 8))
            .First();

        Assert.Equal(2, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverProjectedGroupCountsMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ContainsOverProjectedGroupCountsMatchesLinq));

        bool expected = Numbers().GroupBy(n => n).Select(g => g.Count()).Contains(3);
        bool actual = db.Table<H23fGroupAggregateRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Select(g => g.Count()).Contains(3))
            .First();

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [5, 3, 5, 8, 3, 3];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H23fGroupAggregateRow>().Schema.CreateTable();
        db.Table<H23fGroupAggregateRow>().Add(new H23fGroupAggregateRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
