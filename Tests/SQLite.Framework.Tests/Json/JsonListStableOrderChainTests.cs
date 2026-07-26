using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class StableOrderChainContext : JsonSerializerContext;

[Table("StableOrderChainRows")]
public class StableOrderChainRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonListStableOrderChainTests
{
    [Fact]
    public void ReverseAfterOrderByReversesTiedElements()
    {
        using TestDatabase db = Setup(nameof(ReverseAfterOrderByReversesTiedElements));

        List<int> expected = Nums().OrderBy(n => n % 2).Reverse().ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => r.Nums.OrderBy(n => n % 2).Reverse().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByAfterReverseKeepsTheReversedTieOrder()
    {
        using TestDatabase db = Setup(nameof(OrderByAfterReverseKeepsTheReversedTieOrder));

        List<int> expected = Enumerable.Reverse(Nums()).OrderBy(n => n % 2).ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => Enumerable.Reverse(r.Nums).OrderBy(n => n % 2).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenByAfterReverseSortsBeforeTheReversedTieOrder()
    {
        using TestDatabase db = Setup(nameof(ThenByAfterReverseSortsBeforeTheReversedTieOrder));

        List<int> expected = Enumerable.Reverse(Nums()).OrderBy(n => n % 2).ThenBy(n => n % 3).ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => Enumerable.Reverse(r.Nums).OrderBy(n => n % 2).ThenBy(n => n % 3).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ASecondOrderByKeepsTheFirstAsTheTiebreak()
    {
        using TestDatabase db = Setup(nameof(ASecondOrderByKeepsTheFirstAsTheTiebreak));

        List<int> expected = Nums().OrderBy(n => n % 3).OrderBy(n => n % 2).ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => r.Nums.OrderBy(n => n % 3).OrderBy(n => n % 2).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseTwiceRestoresTheElementOrder()
    {
        using TestDatabase db = Setup(nameof(ReverseTwiceRestoresTheElementOrder));

        List<int> expected = Nums().OrderBy(n => n % 2).Reverse().Reverse().ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => r.Nums.OrderBy(n => n % 2).Reverse().Reverse().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByAfterReverseAndTakeKeepsTheReversedTieOrder()
    {
        using TestDatabase db = Setup(nameof(OrderByAfterReverseAndTakeKeepsTheReversedTieOrder));

        List<int> expected = Nums().OrderBy(n => n % 2).Reverse().Take(4).OrderBy(n => n % 3).ToList();
        List<int> actual = db.Table<StableOrderChainRow>()
            .Select(r => r.Nums.OrderBy(n => n % 2).Reverse().Take(4).OrderBy(n => n % 3).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastAfterOrderByTakesTheFinalTiedElement()
    {
        using TestDatabase db = Setup(nameof(LastAfterOrderByTakesTheFinalTiedElement));

        int expected = Nums().OrderBy(n => n % 2).Last();
        int actual = db.Table<StableOrderChainRow>()
            .Select(r => r.Nums.OrderBy(n => n % 2).Last())
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [10, 21, 12, 23, 14];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(StableOrderChainContext.Default), methodName);
        db.Table<StableOrderChainRow>().Schema.CreateTable();
        db.Table<StableOrderChainRow>().Add(new StableOrderChainRow { Id = 1, Nums = Nums() });
        return db;
    }
}
