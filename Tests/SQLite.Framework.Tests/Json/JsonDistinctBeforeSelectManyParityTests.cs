using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H21hSmGroup
{
    public int Tag { get; set; }

    public List<int> Vals { get; set; } = [];
}

[JsonSerializable(typeof(List<H21hSmGroup>))]
[JsonSerializable(typeof(List<int>))]
public partial class H21hSmContext : JsonSerializerContext;

[Table("H21hSmRows")]
public class H21hSmRow
{
    [Key]
    public int Id { get; set; }

    public List<H21hSmGroup> Groups { get; set; } = [];
}

public class JsonDistinctBeforeSelectManyParityTests
{
    private static List<H21hSmGroup> Groups()
    {
        return
        [
            new H21hSmGroup { Tag = 1, Vals = [5, 6] },
            new H21hSmGroup { Tag = 2, Vals = [6, 7] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hSmContext.Default), methodName);
        db.Table<H21hSmRow>().Schema.CreateTable();
        db.Table<H21hSmRow>().Add(new H21hSmRow { Id = 1, Groups = Groups() });
        return db;
    }

    [Fact]
    public void SelectManyWithoutDistinctMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SelectManyWithoutDistinctMatchesLinq));

        List<int> expected = Groups().SelectMany(g => g.Vals).ToList();
        List<int> actual = db.Table<H21hSmRow>()
            .Select(r => r.Groups.SelectMany(g => g.Vals).ToList()).First();

        Assert.Equal([5, 6, 6, 7], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctThenSelectManyMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctThenSelectManyMatchesLinq));

        List<int> expected = Groups().Distinct().SelectMany(g => g.Vals).ToList();
        List<int> actual = db.Table<H21hSmRow>()
            .Select(r => r.Groups.Distinct().SelectMany(g => g.Vals).ToList()).First();

        Assert.Equal([5, 6, 6, 7], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctThenSelectManyCountMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctThenSelectManyCountMatchesLinq));

        int expected = Groups().Distinct().SelectMany(g => g.Vals).Count();
        int actual = db.Table<H21hSmRow>()
            .Select(r => r.Groups.Distinct().SelectMany(g => g.Vals).Count()).First();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }
}
