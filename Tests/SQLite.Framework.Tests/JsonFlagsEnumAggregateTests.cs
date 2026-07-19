using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum H20JsonPerm
{
    None = 0,
    Read = 1,
    Write = 2,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H20JsonPerm>))]
public partial class H20JsonPermContext : JsonSerializerContext;

[Table("H20JsonPermRows")]
public class H20JsonPermRow
{
    [Key]
    public int Id { get; set; }

    public List<H20JsonPerm> Perms { get; set; } = [];
}

public class JsonFlagsEnumAggregateTests
{
    private static TestDatabase Seed(out List<H20JsonPermRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H20JsonPermContext.Default), methodName);
        db.Table<H20JsonPermRow>().Schema.CreateTable();
        rows =
        [
            new H20JsonPermRow { Id = 1, Perms = [H20JsonPerm.Read, H20JsonPerm.Read | H20JsonPerm.Write] },
            new H20JsonPermRow { Id = 2, Perms = [H20JsonPerm.Read | H20JsonPerm.Write] },
        ];
        db.Table<H20JsonPermRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void MaxOverFlagsEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonPermRow> rows, nameof(MaxOverFlagsEnumListMatchesLinq));

        List<H20JsonPerm> expected = rows.Select(r => r.Perms.Max()).ToList();
        List<H20JsonPerm> actual = db.Table<H20JsonPermRow>().Select(r => r.Perms.Max()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverFlagsEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonPermRow> rows, nameof(MinOverFlagsEnumListMatchesLinq));

        List<H20JsonPerm> expected = rows.Select(r => r.Perms.Min()).ToList();
        List<H20JsonPerm> actual = db.Table<H20JsonPermRow>().Select(r => r.Perms.Min()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByOverFlagsEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonPermRow> rows, nameof(OrderByOverFlagsEnumListMatchesLinq));

        List<List<H20JsonPerm>> expected = rows.Select(r => r.Perms.OrderBy(p => p).ToList()).ToList();
        List<List<H20JsonPerm>> actual = db.Table<H20JsonPermRow>().Select(r => r.Perms.OrderBy(p => p).ToList()).ToList();

        Assert.Equal(expected, actual);
    }
}
