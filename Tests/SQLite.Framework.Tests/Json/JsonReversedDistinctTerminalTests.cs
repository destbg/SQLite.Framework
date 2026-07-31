using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H26lReversedDistinctContext : JsonSerializerContext;

[Table("H26lReversedDistinctRows")]
public class H26lReversedDistinctRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonReversedDistinctTerminalTests
{
    [Fact]
    public void TheFirstValueOfAReversedDistinctListMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheFirstValueOfAReversedDistinctListMatchesInMemory));

        int expected = Numbers().AsEnumerable().Reverse().Distinct().First();
        int actual = db.Table<H26lReversedDistinctRow>()
            .Select(r => r.Numbers.AsEnumerable().Reverse().Distinct().First())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheLastValueOfAReversedDistinctListMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheLastValueOfAReversedDistinctListMatchesInMemory));

        int expected = Numbers().AsEnumerable().Reverse().Distinct().Last();
        int actual = db.Table<H26lReversedDistinctRow>()
            .Select(r => r.Numbers.AsEnumerable().Reverse().Distinct().Last())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheFirstOrDefaultValueOfAReversedDistinctListMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheFirstOrDefaultValueOfAReversedDistinctListMatchesInMemory));

        int expected = Numbers().AsEnumerable().Reverse().Distinct().FirstOrDefault();
        int actual = db.Table<H26lReversedDistinctRow>()
            .Select(r => r.Numbers.AsEnumerable().Reverse().Distinct().FirstOrDefault())
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [4, 9, 4, 9, 4];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H26lReversedDistinctContext.Default), methodName);
        db.Table<H26lReversedDistinctRow>().Schema.CreateTable();
        db.Table<H26lReversedDistinctRow>().Add(new H26lReversedDistinctRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
