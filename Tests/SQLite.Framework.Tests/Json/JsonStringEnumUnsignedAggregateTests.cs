using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21cJsonBigMode : ulong
{
    Small = 1,
    Half = 1UL << 62,
    Top = ulong.MaxValue,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21cJsonBigMode>))]
public partial class H21cJsonBigModeContext : JsonSerializerContext;

[Table("H21cJsonBigModeRows")]
public class H21cJsonBigModeRow
{
    [Key]
    public int Id { get; set; }

    public List<H21cJsonBigMode> Modes { get; set; } = [];
}

public class JsonStringEnumUnsignedAggregateTests
{
    [Fact]
    public void MaxOverUnsignedStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H21cJsonBigModeRow> rows, nameof(MaxOverUnsignedStringEnumListMatchesLinq));

        List<H21cJsonBigMode> expected = rows
            .Select(r => r.Modes.OrderBy(m => unchecked((long)m)).Last())
            .ToList();
        List<H21cJsonBigMode> actual = db.Table<H21cJsonBigModeRow>().Select(r => r.Modes.Max()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverUnsignedStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H21cJsonBigModeRow> rows, nameof(MinOverUnsignedStringEnumListMatchesLinq));

        List<H21cJsonBigMode> expected = rows
            .Select(r => r.Modes.OrderBy(m => unchecked((long)m)).First())
            .ToList();
        List<H21cJsonBigMode> actual = db.Table<H21cJsonBigModeRow>().Select(r => r.Modes.Min()).ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<H21cJsonBigModeRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21cJsonBigModeContext.Default), methodName);
        db.Table<H21cJsonBigModeRow>().Schema.CreateTable();
        rows =
        [
            new H21cJsonBigModeRow { Id = 1, Modes = [H21cJsonBigMode.Half, H21cJsonBigMode.Top] },
            new H21cJsonBigModeRow { Id = 2, Modes = [H21cJsonBigMode.Small, H21cJsonBigMode.Top] },
        ];
        db.Table<H21cJsonBigModeRow>().AddRange(rows);
        return db;
    }
}
