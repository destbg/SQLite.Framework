using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class H25gPagedMapContext : JsonSerializerContext;

[Table("H25gPagedMapRows")]
public class H25gPagedMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = new();
}

public class JsonDictionaryEntryPagingPredicateTests
{
    [Fact]
    public void CountingTakenEntriesWithAValuePredicateMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingTakenEntriesWithAValuePredicateMatchesInMemory));

        int expected = Map().Take(3).Count(e => e.Value > 1);
        int actual = db.Table<H25gPagedMapRow>()
            .Select(r => r.Map.Take(3).Count(e => e.Value > 1))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyOverSkippedEntriesWithAValuePredicateMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(AnyOverSkippedEntriesWithAValuePredicateMatchesInMemory));

        bool expected = Map().Skip(0).Any(e => e.Value > 2);
        bool actual = db.Table<H25gPagedMapRow>()
            .Select(r => r.Map.Skip(0).Any(e => e.Value > 2))
            .First();

        Assert.Equal(expected, actual);
    }

    private static Dictionary<string, int> Map()
    {
        return new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H25gPagedMapContext.Default), methodName);
        db.Table<H25gPagedMapRow>().Schema.CreateTable();
        db.Table<H25gPagedMapRow>().Add(new H25gPagedMapRow { Id = 1, Map = Map() });
        return db;
    }
}
