using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonOuterColumnAggregateBindingTests
{
    private static TestDatabase Seed(out List<H20JsonOuterRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H20JsonOuterContext.Default), methodName);
        db.Table<H20JsonOuterRow>().Schema.CreateTable();
        rows =
        [
            new H20JsonOuterRow { Id = 1, Outer = 7, Plain = 9, Nums = [1, 2, 3] },
            new H20JsonOuterRow { Id = 2, Outer = 8, Plain = 4, Nums = [5] },
        ];
        db.Table<H20JsonOuterRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void SumOverOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(SumOverOuterValueColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Sum(n => r.Outer)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Sum(n => r.Outer)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(MinOverOuterValueColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Min(n => r.Outer)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Min(n => r.Outer)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverOuterPlainColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(SumOverOuterPlainColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Sum(n => r.Plain)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Sum(n => r.Plain)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereSumOverOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(WhereSumOverOuterValueColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Where(n => n > 1).Sum(n => r.Outer)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Where(n => n > 1).Sum(n => r.Outer)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctSumOverOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(DistinctSumOverOuterValueColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Distinct().Sum(n => r.Outer)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Distinct().Sum(n => r.Outer)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeSumOverOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(TakeSumOverOuterValueColumnMatchesLinq));

        List<int> expected = rows.Select(r => r.Nums.Take(2).Sum(n => r.Outer)).ToList();
        List<int> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Take(2).Sum(n => r.Outer)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereSelectOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(WhereSelectOuterValueColumnMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.Where(n => n > 1).Select(n => r.Outer).ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Where(n => n > 1).Select(n => r.Outer).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctSelectOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(DistinctSelectOuterValueColumnMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.Distinct().Select(n => r.Outer).ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Distinct().Select(n => r.Outer).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectDistinctOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(SelectDistinctOuterValueColumnMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.Select(n => r.Outer).Distinct().ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Select(n => r.Outer).Distinct().ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderBySelectOuterValueColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(OrderBySelectOuterValueColumnMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.OrderByDescending(n => n).Select(n => r.Outer).ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.OrderByDescending(n => n).Select(n => r.Outer).ToList()).ToList();

        Assert.Equal(expected, actual);
    }
}
