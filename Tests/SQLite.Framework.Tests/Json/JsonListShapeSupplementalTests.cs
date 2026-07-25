using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class ShapeNumbersRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Values { get; set; } = [];
}

public class JsonListShapeSupplementalTests
{
    private static TestDatabase Seed(out List<int> values)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<ShapeNumbersRow>().Schema.CreateTable();
        values = [5, 3, 5, 8, 3, 3];
        db.Table<ShapeNumbersRow>().Add(new ShapeNumbersRow { Id = 1, Values = values });
        return db;
    }

    [Fact]
    public void OrderedDistinctReversedMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.OrderBy(x => x).Distinct().Reverse().ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.OrderBy(x => x).Distinct().Reverse().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DescendingDistinctReversedMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.OrderByDescending(x => x).Distinct().Reverse().ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.OrderByDescending(x => x).Distinct().Reverse().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedFilterThenReverseMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).Reverse().ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).Reverse().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedFilterThenDistinctMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).Distinct().ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).Distinct().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedFilterPagedThenFilteredMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key)
            .Take(2).Where(k => k >= 0).ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).Take(2).Where(k => k >= 0).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeThenGroupedFilterMatchesLinq()
    {
        using TestDatabase db = Seed(out List<int> values);

        List<int> expected = values.Take(5).GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        List<int> actual = db.Table<ShapeNumbersRow>()
            .Select(r => r.Values.Take(5).GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
