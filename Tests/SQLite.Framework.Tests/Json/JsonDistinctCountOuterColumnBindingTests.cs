using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class H21hDistinctCountContext : JsonSerializerContext;

[Table("H21hDistinctCountRows")]
public class H21hDistinctCountRow
{
    [Key]
    public int Id { get; set; }

    public int Outer { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonDistinctCountOuterColumnBindingTests
{
    private static List<H21hDistinctCountRow> Rows()
    {
        return
        [
            new H21hDistinctCountRow { Id = 1, Outer = 7, Nums = [1, 2, 3] },
            new H21hDistinctCountRow { Id = 2, Outer = 8, Nums = [5] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hDistinctCountContext.Default), methodName);
        db.Table<H21hDistinctCountRow>().Schema.CreateTable();
        db.Table<H21hDistinctCountRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void DistinctCountOverOuterColumnElementMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctCountOverOuterColumnElementMatchesLinq));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Count()).ToList();
        List<int> actual = db.Table<H21hDistinctCountRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Count()).ToList();

        Assert.Equal([1, 1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctLongCountOverOuterColumnElementMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctLongCountOverOuterColumnElementMatchesLinq));

        List<long> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().LongCount()).ToList();
        List<long> actual = db.Table<H21hDistinctCountRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().LongCount()).ToList();

        Assert.Equal([1L, 1L], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctSingleOverOuterColumnElementMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctSingleOverOuterColumnElementMatchesLinq));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Single()).ToList();
        List<int> actual = db.Table<H21hDistinctCountRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Single()).ToList();

        Assert.Equal([7, 8], expected);
        Assert.Equal(expected, actual);
    }
}
