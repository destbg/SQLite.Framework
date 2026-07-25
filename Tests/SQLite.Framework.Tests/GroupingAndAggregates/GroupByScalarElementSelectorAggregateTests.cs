using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GroupedSaleRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class GroupByScalarElementSelectorAggregateTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<GroupedSaleRow>().Schema.CreateTable();
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 1, Name = "a", Value = 10 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 2, Name = "a", Value = 20 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 3, Name = "b", Value = 5 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 4, Name = "b", Value = 30 });
        return db;
    }

    [Fact]
    public void SumWithSelectorAggregatesTheElements()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Sum(v => v * 2))
            .ToList();

        Assert.Equal([60, 70], expected);

        List<int> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Sum(v => v * 2))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredSumAggregatesTheFilteredElements()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Where(v => v > 10).Sum())
            .ToList();

        Assert.Equal([20, 30], expected);

        List<int> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Where(v => v > 10).Sum())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PredicateCountCountsTheMatchingElements()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Count(v => v > 10))
            .ToList();

        Assert.Equal([1, 1], expected);

        List<int> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Count(v => v > 10))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxWithSelectorAggregatesTheElements()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Max(v => v + 1))
            .ToList();

        Assert.Equal([21, 31], expected);

        List<int> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Max(v => v + 1))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AverageWithSelectorAggregatesTheElements()
    {
        using TestDatabase db = SetupDatabase();

        List<double> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Average(v => v))
            .ToList();

        Assert.Equal([15, 17.5], expected);

        List<double> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value)
            .Select(g => g.Average(v => v))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
