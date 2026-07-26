using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21hBigLevel : ulong
{
    Small = 1,
    Huge = ulong.MaxValue,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21hBigLevel>))]
public partial class H21hBigLevelContext : JsonSerializerContext;

[Table("H21hBigLevelRows")]
public class H21hBigLevelRow
{
    [Key]
    public int Id { get; set; }

    public List<H21hBigLevel> Levels { get; set; } = [];
}

public class JsonUnsignedStringEnumOrderingParityTests
{
    private static List<H21hBigLevel> Levels()
    {
        return [H21hBigLevel.Small, H21hBigLevel.Huge];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hBigLevelContext.Default), methodName);
        db.Table<H21hBigLevelRow>().Schema.CreateTable();
        db.Table<H21hBigLevelRow>().Add(new H21hBigLevelRow { Id = 1, Levels = Levels() });
        return db;
    }

    [Fact]
    public void MinOverUnsignedStringEnumListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(MinOverUnsignedStringEnumListMatchesLinq));

        H21hBigLevel expected = Levels().OrderBy(l => unchecked((long)l)).First();
        H21hBigLevel actual = db.Table<H21hBigLevelRow>().Select(r => r.Levels.Min()).First();

        Assert.Equal(H21hBigLevel.Huge, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverUnsignedStringEnumListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(MaxOverUnsignedStringEnumListMatchesLinq));

        H21hBigLevel expected = Levels().OrderBy(l => unchecked((long)l)).Last();
        H21hBigLevel actual = db.Table<H21hBigLevelRow>().Select(r => r.Levels.Max()).First();

        Assert.Equal(H21hBigLevel.Small, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByOverUnsignedStringEnumListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(OrderByOverUnsignedStringEnumListMatchesLinq));

        List<H21hBigLevel> expected = Levels().OrderBy(l => l.ToString(), StringComparer.Ordinal).ToList();
        List<H21hBigLevel> actual = db.Table<H21hBigLevelRow>()
            .Select(r => r.Levels.OrderBy(l => l).ToList()).First();

        Assert.Equal([H21hBigLevel.Huge, H21hBigLevel.Small], expected);
        Assert.Equal(expected, actual);
    }
}
