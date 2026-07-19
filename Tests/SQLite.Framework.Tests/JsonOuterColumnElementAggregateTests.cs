using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<int>))]
public partial class H20JsonOuterContext : JsonSerializerContext;

[Table("H20JsonOuterRows")]
public class H20JsonOuterRow
{
    [Key]
    public int Id { get; set; }

    [Column("value")]
    public int Outer { get; set; }

    public int Plain { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonOuterColumnElementAggregateTests
{
    private static TestDatabase Seed(out List<H20JsonOuterRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H20JsonOuterContext.Default), methodName);
        db.Table<H20JsonOuterRow>().Schema.CreateTable();
        rows =
        [
            new H20JsonOuterRow { Id = 1, Outer = 7, Plain = 9, Nums = [1, 2, 3] },
            new H20JsonOuterRow { Id = 2, Outer = 8, Plain = 4, Nums = [5] },
        ];
        db.Table<H20JsonOuterRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void OuterValueNamedColumnElementMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(OuterValueNamedColumnElementMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.Select(n => r.Outer).ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Select(n => r.Outer).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OuterPlainColumnElementMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonOuterRow> rows, nameof(OuterPlainColumnElementMatchesLinq));

        List<List<int>> expected = rows.Select(r => r.Nums.Select(n => r.Plain).ToList()).ToList();
        List<List<int>> actual = db.Table<H20JsonOuterRow>().Select(r => r.Nums.Select(n => r.Plain).ToList()).ToList();

        Assert.Equal(expected, actual);
    }
}
