using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class H21hAsEnumContext : JsonSerializerContext;

[Table("H21hAsEnumRows")]
public class H21hAsEnumRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonAsEnumerableCollectionBuilderChainTests
{
    private static List<H21hAsEnumRow> Rows()
    {
        return
        [
            new H21hAsEnumRow { Id = 1, Num = 10, Nums = [1, 2, 3] },
            new H21hAsEnumRow { Id = 2, Num = 30, Nums = [5] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hAsEnumContext.Default), methodName);
        db.Table<H21hAsEnumRow>().Schema.CreateTable();
        db.Table<H21hAsEnumRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void AsEnumerableToListContainsOverJsonListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToListContainsOverJsonListMatchesLinq));

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().ToList().Contains(2)).ToList();
        List<bool> actual = db.Table<H21hAsEnumRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().ToList().Contains(2)).ToList();

        Assert.Equal([true, false], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsEnumerableToArrayIndexOfOverJsonListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToArrayIndexOfOverJsonListMatchesLinq));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => Array.IndexOf(r.Nums.AsEnumerable().ToArray(), 2)).ToList();
        List<int> actual = db.Table<H21hAsEnumRow>().OrderBy(r => r.Id)
            .Select(r => Array.IndexOf(r.Nums.AsEnumerable().ToArray(), 2)).ToList();

        Assert.Equal([1, -1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsEnumerableToListContainsOverInlineArrayMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToListContainsOverInlineArrayMatchesLinq));

        List<int> expected = Rows()
            .Where(r => new[] { r.Num, 20 }.AsEnumerable().ToList().Contains(30))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hAsEnumRow>()
            .Where(r => new[] { r.Num, 20 }.AsEnumerable().ToList().Contains(30))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([2], expected);
        Assert.Equal(expected, actual);
    }
}
