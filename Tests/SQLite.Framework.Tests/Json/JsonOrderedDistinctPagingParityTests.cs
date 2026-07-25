using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class PagedNumListContext : JsonSerializerContext;

internal sealed class PagedNumListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonOrderedDistinctPagingParityTests
{
    private static TestDatabase Seed(List<int> nums, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(PagedNumListContext.Default), methodName);
        db.Table<PagedNumListRow>().Schema.CreateTable();
        db.Table<PagedNumListRow>().Add(new PagedNumListRow { Id = 1, Nums = nums });
        return db;
    }

    [Fact]
    public void OrderByWithTiesThenDistinctSkipMatchesLinq()
    {
        List<int> local = [30, 10, 20];
        using TestDatabase db = Seed(local, nameof(OrderByWithTiesThenDistinctSkipMatchesLinq));

        List<int> expected = local.OrderBy(x => x / 100).Distinct().Skip(1).ToList();
        List<int> actual = db.Table<PagedNumListRow>().Select(r => r.Nums.OrderBy(x => x / 100).Distinct().Skip(1).ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseDistinctSkipLastMatchesLinq()
    {
        List<int> local = [5, 3, 5, 8, 3, 3];
        using TestDatabase db = Seed(local, nameof(ReverseDistinctSkipLastMatchesLinq));

        int expected = local.AsEnumerable().Reverse().Distinct().Skip(1).Last();
        int actual = db.Table<PagedNumListRow>().Select(r => r.Nums.AsEnumerable().Reverse().Distinct().Skip(1).Last()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseDistinctSkipReverseMatchesLinq()
    {
        List<int> local = [5, 3, 5, 8, 3, 3];
        using TestDatabase db = Seed(local, nameof(ReverseDistinctSkipReverseMatchesLinq));

        List<int> expected = local.AsEnumerable().Reverse().Distinct().Skip(1).Reverse().ToList();
        List<int> actual = db.Table<PagedNumListRow>().Select(r => r.Nums.AsEnumerable().Reverse().Distinct().Skip(1).Reverse().ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByThenReverseThenDistinctMatchesLinq()
    {
        List<int> local = [1, 5, 2];
        using TestDatabase db = Seed(local, nameof(OrderByThenReverseThenDistinctMatchesLinq));

        List<int> expected = local.OrderBy(x => x).Reverse().Distinct().ToList();
        List<int> actual = db.Table<PagedNumListRow>().Select(r => r.Nums.OrderBy(x => x).Reverse().Distinct().ToList()).First();

        Assert.Equal(expected, actual);
    }
}
