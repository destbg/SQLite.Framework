using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H25gGroupTailContext : JsonSerializerContext;

[Table("H25gGroupTailRows")]
public class H25gGroupTailRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupingLastAndSingleKeyTests
{
    [Fact]
    public void ReadingTheKeyOfTheLastGroupMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheKeyOfTheLastGroupMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 1).Last().Key;
        int actual = db.Table<H25gGroupTailRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 1).Last().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadingTheKeyOfTheGroupAtAnIndexMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheKeyOfTheGroupAtAnIndexMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 1).OrderBy(g => g.Key).ElementAt(1).Key;
        int actual = db.Table<H25gGroupTailRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 1).ElementAt(1).Key)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadingTheKeyOfTheOnlyGroupMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheKeyOfTheOnlyGroupMatchesInMemory));

        int expected = SingleKeyNumbers().GroupBy(n => n % 3 + 1).Single().Key;
        int actual = db.Table<H25gGroupTailRow>()
            .Where(r => r.Id == 2)
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 1).Single().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [30, 11, 22, 60, 41, 52];
    }

    private static List<int> SingleKeyNumbers()
    {
        return [30, 60];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H25gGroupTailContext.Default), methodName);
        db.Table<H25gGroupTailRow>().Schema.CreateTable();
        db.Table<H25gGroupTailRow>().AddRange(
        [
            new H25gGroupTailRow { Id = 1, Numbers = Numbers() },
            new H25gGroupTailRow { Id = 2, Numbers = SingleKeyNumbers() }
        ]);
        return db;
    }
}
