using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21cJsonLevel
{
    Bravo = 1,
    Alpha = 2,
    Charlie = 3,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21cJsonLevel>))]
public partial class H21cJsonLevelContext : JsonSerializerContext;

[Table("H21cJsonLevelRows")]
public class H21cJsonLevelRow
{
    [Key]
    public int Id { get; set; }

    public List<H21cJsonLevel> Levels { get; set; } = [];
}

public class JsonStringEnumNumericCastTests
{
    [Fact]
    public void SumOverNumericCastElementsMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H21cJsonLevelRow> rows, nameof(SumOverNumericCastElementsMatchesLinq));

        List<int> expected = rows.Select(r => r.Levels.Sum(g => (int)g)).ToList();
        List<int> actual = db.Table<H21cJsonLevelRow>().Select(r => r.Levels.Sum(g => (int)g)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountFilteredByNumericCastElementMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H21cJsonLevelRow> rows, nameof(CountFilteredByNumericCastElementMatchesLinq));

        List<int> expected = rows.Select(r => r.Levels.Where(g => (int)g > 1).Count()).ToList();
        List<int> actual = db.Table<H21cJsonLevelRow>().Select(r => r.Levels.Where(g => (int)g > 1).Count()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByNumericCastElementMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H21cJsonLevelRow> rows, nameof(OrderByNumericCastElementMatchesLinq));

        List<List<H21cJsonLevel>> expected = rows.Select(r => r.Levels.OrderBy(g => (int)g).ToList()).ToList();
        List<List<H21cJsonLevel>> actual = db.Table<H21cJsonLevelRow>().Select(r => r.Levels.OrderBy(g => (int)g).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<H21cJsonLevelRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21cJsonLevelContext.Default), methodName);
        db.Table<H21cJsonLevelRow>().Schema.CreateTable();
        rows =
        [
            new H21cJsonLevelRow { Id = 1, Levels = [H21cJsonLevel.Bravo, H21cJsonLevel.Alpha] },
            new H21cJsonLevelRow { Id = 2, Levels = [H21cJsonLevel.Charlie, H21cJsonLevel.Bravo] },
        ];
        db.Table<H21cJsonLevelRow>().AddRange(rows);
        return db;
    }
}
