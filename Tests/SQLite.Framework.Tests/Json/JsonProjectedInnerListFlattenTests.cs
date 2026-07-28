using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H23fTagBox
{
    public string Name { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];
}

[JsonSerializable(typeof(List<H23fTagBox>))]
public partial class H23fFlattenContext : JsonSerializerContext;

[Table("H23fFlattenRows")]
public class H23fFlattenRow
{
    [Key]
    public int Id { get; set; }

    public List<H23fTagBox> Boxes { get; set; } = [];
}

public class JsonProjectedInnerListFlattenTests
{
    [Fact]
    public void FlatteningAProjectedInnerListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(FlatteningAProjectedInnerListMatchesLinq));

        List<string> expected = Boxes().Select(b => b.Tags).SelectMany(t => t).ToList();
        List<string> actual = db.Table<H23fFlattenRow>()
            .Select(r => r.Boxes.Select(b => b.Tags).SelectMany(t => t).ToList())
            .First();

        Assert.Equal(["a", "b", "c"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountingAFlattenedProjectedInnerListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(CountingAFlattenedProjectedInnerListMatchesLinq));

        int expected = Boxes().Select(b => b.Tags).SelectMany(t => t).Count();
        int actual = db.Table<H23fFlattenRow>()
            .Select(r => r.Boxes.Select(b => b.Tags).SelectMany(t => t).Count())
            .First();

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    private static List<H23fTagBox> Boxes()
    {
        return
        [
            new H23fTagBox { Name = "first", Tags = ["a", "b"] },
            new H23fTagBox { Name = "second", Tags = ["c"] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H23fFlattenContext.Default), methodName);
        db.Table<H23fFlattenRow>().Schema.CreateTable();
        db.Table<H23fFlattenRow>().Add(new H23fFlattenRow { Id = 1, Boxes = Boxes() });
        return db;
    }
}
