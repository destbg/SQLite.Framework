using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H24fRegroupContext : JsonSerializerContext;

[Table("H24fRegroupRows")]
public class H24fRegroupRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonNestedGroupByChainTests
{
    [Fact]
    public void CountingASecondGroupingBuiltFromTheKeysOfTheFirstMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingASecondGroupingBuiltFromTheKeysOfTheFirstMatchesInMemory));

        List<int> expected = Numbers()
            .GroupBy(n => n % 3)
            .Select(g => g.Key)
            .GroupBy(k => k % 2)
            .Select(outer => outer.Count())
            .ToList();
        List<int> actual = db.Table<H24fRegroupRow>()
            .Select(r => r.Numbers
                .GroupBy(n => n % 3)
                .Select(g => g.Key)
                .GroupBy(k => k % 2)
                .Select(outer => outer.Count()))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeysOfASecondGroupingBuiltFromTheKeysOfTheFirstMatchInMemory()
    {
        using TestDatabase db = Setup(nameof(KeysOfASecondGroupingBuiltFromTheKeysOfTheFirstMatchInMemory));

        List<int> expected = Numbers()
            .GroupBy(n => n % 3)
            .Select(g => g.Key)
            .GroupBy(k => k % 2)
            .Select(outer => outer.Key)
            .ToList();
        List<int> actual = db.Table<H24fRegroupRow>()
            .Select(r => r.Numbers
                .GroupBy(n => n % 3)
                .Select(g => g.Key)
                .GroupBy(k => k % 2)
                .Select(outer => outer.Key))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountingASecondGroupingBuiltFromAPagedFirstGroupingMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingASecondGroupingBuiltFromAPagedFirstGroupingMatchesInMemory));

        List<int> expected = Numbers()
            .GroupBy(n => n % 3)
            .Take(10)
            .Select(g => g.Key)
            .GroupBy(k => k % 2)
            .Select(outer => outer.Count())
            .ToList();
        List<int> actual = db.Table<H24fRegroupRow>()
            .Select(r => r.Numbers
                .GroupBy(n => n % 3)
                .Take(10)
                .Select(g => g.Key)
                .GroupBy(k => k % 2)
                .Select(outer => outer.Count()))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [3, 1, 2, 6, 4, 5];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H24fRegroupContext.Default), methodName);
        db.Table<H24fRegroupRow>().Schema.CreateTable();
        db.Table<H24fRegroupRow>().Add(new H24fRegroupRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
