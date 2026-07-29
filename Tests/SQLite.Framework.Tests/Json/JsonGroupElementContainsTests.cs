using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H24fGroupContainsContext : JsonSerializerContext;

[Table("H24fGroupContainsRows")]
public class H24fGroupContainsRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupElementContainsTests
{
    [Fact]
    public void CountingGroupsHoldingAValueMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingGroupsHoldingAValueMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 2).Count(g => g.Contains(3));
        int actual = db.Table<H24fGroupContainsRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 2).Count(g => g.Contains(3)))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeysOfGroupsHoldingAValueMatchInMemory()
    {
        using TestDatabase db = Setup(nameof(KeysOfGroupsHoldingAValueMatchInMemory));

        List<int> expected = Numbers().GroupBy(n => n % 2).Where(g => g.Contains(3)).Select(g => g.Key).ToList();
        List<int> actual = db.Table<H24fGroupContainsRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 2).Where(g => g.Contains(3)).Select(g => g.Key))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [1, 2, 3, 4];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H24fGroupContainsContext.Default), methodName);
        db.Table<H24fGroupContainsRow>().Schema.CreateTable();
        db.Table<H24fGroupContainsRow>().Add(new H24fGroupContainsRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
