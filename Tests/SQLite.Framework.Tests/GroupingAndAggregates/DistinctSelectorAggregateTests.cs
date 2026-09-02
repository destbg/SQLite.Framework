using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DistinctSelectorAggregateTests
{
    private static TestDatabase Seed(params int[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = values[i] });
        }

        return db;
    }

    [Fact]
    public void DistinctThenSumWithNonInjectiveSelectorMatchesObjects()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        int oracle = new[] { 3, 13, 23, 5 }.Distinct().Sum(a => a % 10);
        Assert.Equal(14, oracle);

        int actual = db.Table<NumericType>().Select(x => x.IntValue).Distinct().Sum(a => a % 10);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSumOfConstantMatchesObjects()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        int oracle = new[] { 3, 13, 23, 5 }.Distinct().Sum(a => 1);
        Assert.Equal(4, oracle);

        int actual = db.Table<NumericType>().Select(x => x.IntValue).Distinct().Sum(a => 1);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenAverageWithNonInjectiveSelectorMatchesObjects()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        double oracle = new[] { 3, 13, 23, 5 }.Distinct().Average(a => a % 10);
        Assert.Equal(3.5, oracle);

        double actual = db.Table<NumericType>().Select(x => x.IntValue).Distinct().Average(a => a % 10);

        Assert.Equal(oracle, actual);
    }
}
