using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25aGroupedAggregateRows")]
public class H25aGroupedAggregateRow
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

public static class H25aGroupedAggregateFns
{
    public static int Negate(int value)
    {
        return -value;
    }
}

public class GroupedInMemoryProjectionAggregateTests
{
    [Fact]
    public void MinOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(MinOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues));

        int expected = Rows()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Min();

        AssertValueOrRefusal(expected, () => db.Table<H25aGroupedAggregateRow>()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Min());
    }

    [Fact]
    public void MaxOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(MaxOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues));

        int expected = Rows()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Max();

        AssertValueOrRefusal(expected, () => db.Table<H25aGroupedAggregateRow>()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Max());
    }

    [Fact]
    public void SumOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SumOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues));

        int expected = Rows()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Sum();

        AssertValueOrRefusal(expected, () => db.Table<H25aGroupedAggregateRow>()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Sum());
    }

    [Fact]
    public void AverageOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(AverageOverAGroupedProjectionThatRunsInMemoryUsesTheProjectedValues));

        double expected = Rows()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Average();

        AssertValueOrRefusal(expected, () => db.Table<H25aGroupedAggregateRow>()
            .GroupBy(r => r.K)
            .Select(g => H25aGroupedAggregateFns.Negate(g.Key))
            .Average());
    }

    private static void AssertValueOrRefusal<T>(T expected, Func<T> run)
    {
        T actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H25aGroupedAggregateRow> Rows()
    {
        return
        [
            new H25aGroupedAggregateRow { Id = 1, K = 1 },
            new H25aGroupedAggregateRow { Id = 2, K = 1 },
            new H25aGroupedAggregateRow { Id = 3, K = 2 },
            new H25aGroupedAggregateRow { Id = 4, K = 3 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25aGroupedAggregateRow>().Schema.CreateTable();
        db.Table<H25aGroupedAggregateRow>().AddRange(Rows());
        return db;
    }
}
