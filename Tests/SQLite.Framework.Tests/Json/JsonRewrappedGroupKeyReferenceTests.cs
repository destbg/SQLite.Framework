using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H24fGroupKeyContext : JsonSerializerContext;

[Table("H24fGroupKeyRows")]
public class H24fGroupKeyRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonRewrappedGroupKeyReferenceTests
{
    [Fact]
    public void GroupKeysFilteredAfterTwoPagingStepsReadTheGroupKeys()
    {
        using TestDatabase db = Setup(nameof(GroupKeysFilteredAfterTwoPagingStepsReadTheGroupKeys));

        List<int> expected = Numbers()
            .GroupBy(n => n % 3)
            .Take(10)
            .Skip(0)
            .Where(g => g.Key > 0)
            .Select(g => g.Key)
            .ToList();
        List<int> actual = db.Table<H24fGroupKeyRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3).Take(10).Skip(0).Where(g => g.Key > 0).Select(g => g.Key))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupKeysAfterDistinctReadTheGroupKeys()
    {
        using TestDatabase db = Setup(nameof(GroupKeysAfterDistinctReadTheGroupKeys));

        List<int> expected = Numbers().GroupBy(n => n % 3).Distinct().Select(g => g.Key).ToList();
        List<int> actual = db.Table<H24fGroupKeyRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3).Distinct().Select(g => g.Key))
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
        TestDatabase db = new(b => b.AddJsonContext(H24fGroupKeyContext.Default), methodName);
        db.Table<H24fGroupKeyRow>().Schema.CreateTable();
        db.Table<H24fGroupKeyRow>().Add(new H24fGroupKeyRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
