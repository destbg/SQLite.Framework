using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(int[]))]
public partial class UntranslatableSourceContext : JsonSerializerContext;

[Table("UntranslatableSourceRows")]
public class UntranslatableSourceRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonCollectionUntranslatableSourceTests
{
    [Fact]
    public void ListContainsOverAClientBuiltSourceMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ListContainsOverAClientBuiltSourceMatchesLinq));

        List<int> expected = Rows()
            .Where(r => Build(r.Num).Contains(20))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<UntranslatableSourceRow>()
            .AsEnumerable()
            .Where(r => Build(r.Num).Contains(20))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ListContainsOverAClientBuiltSourceInAQueryThrows()
    {
        using TestDatabase db = Setup(nameof(ListContainsOverAClientBuiltSourceInAQueryThrows));

        Assert.ThrowsAny<Exception>(() => db.Table<UntranslatableSourceRow>()
            .Where(r => Build(r.Num).Contains(20))
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void InlineListContainsKeepsTheJsonPathWhenTheTypeIsARegisteredCollection()
    {
        using TestDatabase db = Setup(nameof(InlineListContainsKeepsTheJsonPathWhenTheTypeIsARegisteredCollection));

        List<int> expected = Rows()
            .Where(r => new List<int> { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<UntranslatableSourceRow>()
            .Where(r => new List<int> { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineArrayContainsUsesThePlainInList()
    {
        using TestDatabase db = Setup(nameof(InlineArrayContainsUsesThePlainInList));

        List<int> expected = Rows()
            .Where(r => new[] { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<UntranslatableSourceRow>()
            .Where(r => new[] { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayExistsOverAClientBuiltRegisteredArrayInAQueryThrows()
    {
        using TestDatabase db = Setup(nameof(ArrayExistsOverAClientBuiltRegisteredArrayInAQueryThrows));

        Assert.ThrowsAny<Exception>(() => db.Table<UntranslatableSourceRow>()
            .Where(r => Array.Exists(BuildArray(r.Num), v => v > 1))
            .Select(r => r.Id)
            .ToList());
    }

    private static List<int> Build(int seed)
    {
        return [seed];
    }

    private static int[] BuildArray(int seed)
    {
        return [seed];
    }

    private static List<UntranslatableSourceRow> Rows()
    {
        return
        [
            new UntranslatableSourceRow { Id = 1, Num = 10, Nums = [1] },
            new UntranslatableSourceRow { Id = 2, Num = 20, Nums = [2] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(UntranslatableSourceContext.Default), methodName);
        db.Table<UntranslatableSourceRow>().Schema.CreateTable();
        db.Table<UntranslatableSourceRow>().AddRange(Rows());
        return db;
    }
}
