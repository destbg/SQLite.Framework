using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class H24fGroupTerminalContext : JsonSerializerContext;

[Table("H24fGroupTerminalRows")]
public class H24fGroupTerminalRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupTerminalKeyReadTests
{
    [Fact]
    public void ReadingTheKeyOfTheFirstGroupMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(ReadingTheKeyOfTheFirstGroupMatchesInMemory));

        int expected = Numbers().GroupBy(n => n % 3 + 1).First().Key;
        int actual = db.Table<H24fGroupTerminalRow>()
            .Select(r => r.Numbers.GroupBy(n => n % 3 + 1).First().Key)
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [3, 1, 2, 6, 4, 5];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H24fGroupTerminalContext.Default), methodName);
        db.Table<H24fGroupTerminalRow>().Schema.CreateTable();
        db.Table<H24fGroupTerminalRow>().Add(new H24fGroupTerminalRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
