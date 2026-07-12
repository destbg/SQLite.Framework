using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum SnFruit
{
    Pear = 0,
    Apple = 1,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<SnFruit>))]
public partial class SnFruitContext : JsonSerializerContext;

[Table("SnBasketRows")]
public class SnBasketRow
{
    [Key]
    public int Id { get; set; }

    public List<SnFruit> Fruits { get; set; } = [];
}

public class JsonStringEnumAggregateTests
{
    private static TestDatabase Seed(out List<SnBasketRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(SnFruitContext.Default), methodName);
        db.Table<SnBasketRow>().Schema.CreateTable();
        rows = [new SnBasketRow { Id = 1, Fruits = [SnFruit.Pear, SnFruit.Apple] }];
        db.Table<SnBasketRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void MaxOverStringEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<SnBasketRow> rows, nameof(MaxOverStringEnumJsonListMatchesLinq));

        SnFruit expected = rows[0].Fruits.Max();
        SnFruit actual = db.Table<SnBasketRow>().Select(r => r.Fruits.Max()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverStringEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<SnBasketRow> rows, nameof(MinOverStringEnumJsonListMatchesLinq));

        SnFruit expected = rows[0].Fruits.Min();
        SnFruit actual = db.Table<SnBasketRow>().Select(r => r.Fruits.Min()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinAfterWhereOverStringEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<SnBasketRow> rows, nameof(MinAfterWhereOverStringEnumJsonListMatchesLinq));

        SnFruit expected = rows[0].Fruits.Where(f => f >= SnFruit.Pear).Min();
        SnFruit actual = db.Table<SnBasketRow>().Select(r => r.Fruits.Where(f => f >= SnFruit.Pear).Min()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByOverStringEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<SnBasketRow> rows, nameof(OrderByOverStringEnumJsonListMatchesLinq));

        List<SnFruit> expected = rows[0].Fruits.OrderByDescending(f => f).ToList();
        List<SnFruit> actual = db.Table<SnBasketRow>().Select(r => r.Fruits.OrderByDescending(f => f).ToList()).First();

        Assert.Equal(expected, actual);
    }
}
