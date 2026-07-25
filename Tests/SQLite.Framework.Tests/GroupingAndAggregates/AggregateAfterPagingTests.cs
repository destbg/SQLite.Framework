using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AggregateAfterPagingTests
{
    [Fact]
    public void SumWithSelectorAfterOrderByTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        int expected = mem.OrderBy(r => r.Value).Take(3).Sum(r => r.Value);
        int actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Take(3).Sum(r => r.Value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOfScalarProjectionAfterTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        int expected = mem.OrderBy(r => r.Value).Select(r => r.Value).Take(3).Sum();
        int actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Select(r => r.Value).Take(3).Sum();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterSkipTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        int expected = mem.OrderBy(r => r.Value).Skip(2).Take(3).Count();
        int actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Skip(2).Take(3).Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountWithPredicateAfterTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        int expected = mem.OrderBy(r => r.Value).Take(4).Count(r => r.Value > 20);
        int actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Take(4).Count(r => r.Value > 20);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinAfterOrderByTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        int expected = mem.OrderBy(r => r.Value).Take(3).Min(r => r.Value);
        int actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Take(3).Min(r => r.Value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxAfterSkip()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        double expected = mem.OrderBy(r => r.Value).Skip(2).Max(r => r.Price);
        double actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Skip(2).Max(r => r.Price);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AverageAfterOrderByTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        double expected = mem.OrderBy(r => r.Value).Take(3).Average(r => r.Value);
        double actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Take(3).Average(r => r.Value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountAfterTake()
    {
        using TestDatabase db = Seed(out List<AggPagingRow> mem);

        long expected = mem.OrderBy(r => r.Value).Take(3).LongCount();
        long actual = db.Table<AggPagingRow>().OrderBy(r => r.Value).Take(3).LongCount();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<AggPagingRow> mem)
    {
        TestDatabase db = new();
        db.Table<AggPagingRow>().Schema.CreateTable();
        mem = new List<AggPagingRow>
        {
            new() { Id = 1, Value = 50, Price = 5.5, Name = "a" },
            new() { Id = 2, Value = 10, Price = 1.0, Name = "b" },
            new() { Id = 3, Value = 30, Price = 3.0, Name = "c" },
            new() { Id = 4, Value = 40, Price = 4.0, Name = "d" },
            new() { Id = 5, Value = 20, Price = 2.0, Name = "e" },
            new() { Id = 6, Value = 60, Price = 6.0, Name = "f" },
        };
        foreach (AggPagingRow r in mem)
        {
            db.Table<AggPagingRow>().Add(r);
        }

        return db;
    }
}

[Table("AggPagingRows")]
public class AggPagingRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public double Price { get; set; }

    public required string Name { get; set; }
}
