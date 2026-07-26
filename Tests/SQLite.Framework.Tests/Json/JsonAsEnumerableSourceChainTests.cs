using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class AsEnumerableSourceContext : JsonSerializerContext;

[Table("AsEnumerableSourceRows")]
public class AsEnumerableSourceRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonAsEnumerableSourceChainTests
{
    [Fact]
    public void AsEnumerableCountMatchesTheSourceList()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableCountMatchesTheSourceList));

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Nums.AsEnumerable().Count()).ToList();
        List<int> actual = db.Table<AsEnumerableSourceRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().Count()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsEnumerableToListIndexReadsTheSourceElement()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToListIndexReadsTheSourceElement));

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Nums.AsEnumerable().ToList()[0]).ToList();
        List<int> actual = db.Table<AsEnumerableSourceRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().ToList()[0]).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsEnumerableToArrayLengthMatchesTheSourceList()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToArrayLengthMatchesTheSourceList));

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Nums.AsEnumerable().ToArray().Length).ToList();
        List<int> actual = db.Table<AsEnumerableSourceRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().ToArray().Length).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsEnumerableToListExistsMatchesTheSourceList()
    {
        using TestDatabase db = Setup(nameof(AsEnumerableToListExistsMatchesTheSourceList));

        List<bool> expected = Rows().OrderBy(r => r.Id).Select(r => r.Nums.AsEnumerable().ToList().Exists(n => n > 2)).ToList();
        List<bool> actual = db.Table<AsEnumerableSourceRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.AsEnumerable().ToList().Exists(n => n > 2)).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<AsEnumerableSourceRow> Rows()
    {
        return
        [
            new AsEnumerableSourceRow { Id = 1, Nums = [1, 2, 3] },
            new AsEnumerableSourceRow { Id = 2, Nums = [5] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(AsEnumerableSourceContext.Default), methodName);
        db.Table<AsEnumerableSourceRow>().Schema.CreateTable();
        db.Table<AsEnumerableSourceRow>().AddRange(Rows());
        return db;
    }
}
