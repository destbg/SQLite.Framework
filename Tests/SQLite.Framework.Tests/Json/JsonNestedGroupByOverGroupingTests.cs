using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H25gNestedGroupContext : JsonSerializerContext;

[Table("H25gNestedGroupRows")]
public class H25gNestedGroupRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonNestedGroupByOverGroupingTests
{
    [Fact]
    public void CountingGroupsOfGroupsMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingGroupsOfGroupsMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3).GroupBy(g => g.Key % 2).Count();
        int actual = db.Table<H25gNestedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3).GroupBy(g => g.Key % 2).Count())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SummingTheKeysOfGroupsOfGroupsMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(SummingTheKeysOfGroupsOfGroupsMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3).GroupBy(g => g.Key % 2).Sum(o => o.Key);
        int actual = db.Table<H25gNestedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3).GroupBy(g => g.Key % 2).Sum(o => o.Key))
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [30, 11, 22, 60, 41, 52];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H25gNestedGroupContext.Default), methodName);
        db.Table<H25gNestedGroupRow>().Schema.CreateTable();
        db.Table<H25gNestedGroupRow>().Add(new H25gNestedGroupRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
