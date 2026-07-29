using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class H24fMapContext : JsonSerializerContext;

[Table("H24fMapRows")]
public class H24fMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = new();
}

public class JsonDictionaryEnumerableOperatorParityTests
{
    [Fact]
    public void AnyOverADictionaryMemberSeesItsEntries()
    {
        using TestDatabase db = Setup(nameof(AnyOverADictionaryMemberSeesItsEntries));

        List<bool> expected = Rows().Select(r => r.Map.Any()).ToList();
        List<bool> actual = db.Table<H24fMapRow>().OrderBy(r => r.Id).Select(r => r.Map.Any()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOverADictionaryMemberCountsItsEntries()
    {
        using TestDatabase db = Setup(nameof(CountOverADictionaryMemberCountsItsEntries));

        List<int> expected = Rows().Select(r => r.Map.Count()).ToList();
        List<int> actual = db.Table<H24fMapRow>().OrderBy(r => r.Id).Select(r => r.Map.Count()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyWithAValuePredicateOverADictionaryMemberSeesItsEntries()
    {
        using TestDatabase db = Setup(nameof(AnyWithAValuePredicateOverADictionaryMemberSeesItsEntries));

        List<bool> expected = Rows().Select(r => r.Map.Any(entry => entry.Value > 1)).ToList();
        List<bool> actual = db.Table<H24fMapRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Map.Any(entry => entry.Value > 1))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24fMapRow> Rows()
    {
        return
        [
            new H24fMapRow { Id = 1, Map = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } },
            new H24fMapRow { Id = 2, Map = new Dictionary<string, int>() }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H24fMapContext.Default), methodName);
        db.Table<H24fMapRow>().Schema.CreateTable();
        db.Table<H24fMapRow>().AddRange(Rows());
        return db;
    }
}
