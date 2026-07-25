using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class H21hAliasTextContext : JsonSerializerContext;

[Table("H21hAliasTextRows")]
public class H21hAliasTextRow
{
    [Key]
    public int Id { get; set; }

    [Column("j0.j1.j2.j3.j4.v")]
    public int Outer { get; set; }

    public int Plain { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonAggregateInnerReferenceAliasTextTests
{
    private static List<H21hAliasTextRow> Rows()
    {
        return
        [
            new H21hAliasTextRow { Id = 1, Outer = 7, Plain = 7, Nums = [1, 2, 3] },
            new H21hAliasTextRow { Id = 2, Outer = 8, Plain = 8, Nums = [5] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hAliasTextContext.Default), methodName);
        db.Table<H21hAliasTextRow>().Schema.CreateTable();
        db.Table<H21hAliasTextRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void PlainColumnNameElementListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(PlainColumnNameElementListMatchesLinq));

        List<List<int>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Plain).ToList()).ToList();
        List<List<int>> actual = db.Table<H21hAliasTextRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Plain).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DottedColumnNameElementListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DottedColumnNameElementListMatchesLinq));

        List<List<int>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).ToList()).ToList();
        List<List<int>> actual = db.Table<H21hAliasTextRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DottedColumnNameElementSumMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DottedColumnNameElementSumMatchesLinq));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Sum(n => r.Outer)).ToList();
        List<int> actual = db.Table<H21hAliasTextRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Sum(n => r.Outer)).ToList();

        Assert.Equal([21, 8], expected);
        Assert.Equal(expected, actual);
    }
}
