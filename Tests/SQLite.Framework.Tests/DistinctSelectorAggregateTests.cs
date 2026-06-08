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
    public void DistinctThenSumWithNonInjectiveSelectorThrows()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        int oracle = new[] { 3, 13, 23, 5 }.Distinct().Sum(a => a % 10);
        Assert.Equal(14, oracle);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<NumericType>().Select(x => x.IntValue).Distinct().Sum(a => a % 10));
    }

    [Fact]
    public void DistinctThenSumOfConstantThrows()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        int oracle = new[] { 3, 13, 23, 5 }.Distinct().Sum(a => 1);
        Assert.Equal(4, oracle);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<NumericType>().Select(x => x.IntValue).Distinct().Sum(a => 1));
    }

    [Fact]
    public void DistinctThenAverageWithNonInjectiveSelectorThrows()
    {
        using TestDatabase db = Seed(3, 13, 23, 5);

        double oracle = new[] { 3, 13, 23, 5 }.Distinct().Average(a => a % 10);
        Assert.Equal(3.5, oracle);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<NumericType>().Select(x => x.IntValue).Distinct().Average(a => a % 10));
    }
}
