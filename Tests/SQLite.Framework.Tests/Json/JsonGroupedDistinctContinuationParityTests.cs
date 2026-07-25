using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class H21hGroupedDistinctContext : JsonSerializerContext;

[Table("H21hGroupedDistinctRows")]
public class H21hGroupedDistinctRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupedDistinctContinuationParityTests
{
    private static List<int> Source()
    {
        return [2, 1, 4, 3];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hGroupedDistinctContext.Default), methodName);
        db.Table<H21hGroupedDistinctRow>().Schema.CreateTable();
        db.Table<H21hGroupedDistinctRow>().Add(new H21hGroupedDistinctRow { Id = 1, Nums = Source() });
        return db;
    }

    [Fact]
    public void GroupKeyDistinctThenSelectMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(GroupKeyDistinctThenSelectMatchesLinq));

        List<int> expected = Source().GroupBy(x => x % 2).Select(g => g.Key).Distinct().Select(k => k * 2)
            .OrderBy(v => v).ToList();
        List<int> actual = db.Table<H21hGroupedDistinctRow>()
            .Select(r => r.Nums.GroupBy(x => x % 2).Select(g => g.Key).Distinct().Select(k => k * 2).ToList())
            .First()
            .OrderBy(v => v).ToList();

        Assert.Equal([0, 2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupKeyDistinctThenTakeMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(GroupKeyDistinctThenTakeMatchesLinq));

        List<int> expected = Source().GroupBy(x => x % 2).Select(g => g.Key).Distinct().Take(1).ToList();
        List<int> actual = db.Table<H21hGroupedDistinctRow>()
            .Select(r => r.Nums.GroupBy(x => x % 2).Select(g => g.Key).Distinct().Take(1).ToList())
            .First();

        Assert.Equal([0], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupHavingKeyDistinctThenSelectMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(GroupHavingKeyDistinctThenSelectMatchesLinq));

        List<int> expected = Source().GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key)
            .Distinct().Select(k => k + 100).OrderBy(v => v).ToList();
        List<int> actual = db.Table<H21hGroupedDistinctRow>()
            .Select(r => r.Nums.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Key)
                .Distinct().Select(k => k + 100).ToList())
            .First()
            .OrderBy(v => v).ToList();

        Assert.Equal([100, 101], expected);
        Assert.Equal(expected, actual);
    }
}
