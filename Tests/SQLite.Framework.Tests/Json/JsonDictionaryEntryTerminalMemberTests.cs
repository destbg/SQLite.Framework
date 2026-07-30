using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class H25gEntryMapContext : JsonSerializerContext;

[Table("H25gEntryMapRows")]
public class H25gEntryMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = new();
}

public class JsonDictionaryEntryTerminalMemberTests
{
    [Fact]
    public void ReadingTheKeyOfTheHighestValuedEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheKeyOfTheHighestValuedEntryMatchesInMemory));

        string expected = Map().OrderByDescending(e => e.Value).First().Key;
        string actual = db.Table<H25gEntryMapRow>()
            .Select(r => r.Map.OrderByDescending(e => e.Value).First().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadingTheValueOfTheLowestValuedEntryMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheValueOfTheLowestValuedEntryMatchesInMemory));

        int expected = Map().OrderBy(e => e.Value).First().Value;
        int actual = db.Table<H25gEntryMapRow>()
            .Select(r => r.Map.OrderBy(e => e.Value).First().Value)
            .First();

        Assert.Equal(expected, actual);
    }

    private static Dictionary<string, int> Map()
    {
        return new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H25gEntryMapContext.Default), methodName);
        db.Table<H25gEntryMapRow>().Schema.CreateTable();
        db.Table<H25gEntryMapRow>().Add(new H25gEntryMapRow { Id = 1, Map = Map() });
        return db;
    }
}
