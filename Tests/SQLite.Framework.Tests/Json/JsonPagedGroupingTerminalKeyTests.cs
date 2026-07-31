using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H26lPagedGroupContext : JsonSerializerContext;

[Table("H26lPagedGroupRows")]
public class H26lPagedGroupRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonPagedGroupingTerminalKeyTests
{
    [Fact]
    public void TheKeyOfTheFirstGroupAfterTakeMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheKeyOfTheFirstGroupAfterTakeMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 10).Take(2).First().Key;
        int actual = db.Table<H26lPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 10).Take(2).First().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheKeyOfTheFirstGroupAfterSkipMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheKeyOfTheFirstGroupAfterSkipMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 10).Skip(1).First().Key;
        int actual = db.Table<H26lPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 10).Skip(1).First().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheKeyOfTheLastGroupAfterTakeMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheKeyOfTheLastGroupAfterTakeMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 10).Take(2).Last().Key;
        int actual = db.Table<H26lPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 10).Take(2).Last().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheKeyOfTheGroupAtAnIndexAfterTakeMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(TheKeyOfTheGroupAtAnIndexAfterTakeMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 10).Take(3).ElementAt(1).Key;
        int actual = db.Table<H26lPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 10).Take(3).ElementAt(1).Key)
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [3, 1, 2, 6, 4, 5];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H26lPagedGroupContext.Default), methodName);
        db.Table<H26lPagedGroupRow>().Schema.CreateTable();
        db.Table<H26lPagedGroupRow>().Add(new H26lPagedGroupRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
