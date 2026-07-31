using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class H26lDirectEntryMapContext : JsonSerializerContext;

[Table("H26lDirectEntryMapRows")]
public class H26lDirectEntryMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = new();
}

public class JsonDictionaryDirectTerminalEntryTests
{
    [Fact]
    public void TheValueOfTheFirstEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheValueOfTheFirstEntryMatchesInMemory));

        int expected = ThreeEntries().First().Value;
        int actual = db.Table<H26lDirectEntryMapRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Map.First().Value)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheKeyOfTheFirstEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheKeyOfTheFirstEntryMatchesInMemory));

        string expected = ThreeEntries().First().Key;
        string actual = db.Table<H26lDirectEntryMapRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Map.First().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheValueOfTheLastEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheValueOfTheLastEntryMatchesInMemory));

        int expected = ThreeEntries().Last().Value;
        int actual = db.Table<H26lDirectEntryMapRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Map.Last().Value)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheValueOfTheOnlyEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheValueOfTheOnlyEntryMatchesInMemory));

        int expected = OneEntry().Single().Value;
        int actual = db.Table<H26lDirectEntryMapRow>()
            .Where(r => r.Id == 2)
            .Select(r => r.Map.Single().Value)
            .First();

        Assert.Equal(expected, actual);
    }

    private static Dictionary<string, int> ThreeEntries()
    {
        return new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    private static Dictionary<string, int> OneEntry()
    {
        return new Dictionary<string, int> { ["z"] = 9 };
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H26lDirectEntryMapContext.Default), methodName);
        db.Table<H26lDirectEntryMapRow>().Schema.CreateTable();
        db.Table<H26lDirectEntryMapRow>().AddRange(
        [
            new H26lDirectEntryMapRow { Id = 1, Map = ThreeEntries() },
            new H26lDirectEntryMapRow { Id = 2, Map = OneEntry() }
        ]);
        return db;
    }
}
