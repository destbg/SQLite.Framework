using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<DateTime>))]
[JsonSerializable(typeof(DateTime[]))]
public partial class H22fInlineListContext : JsonSerializerContext;

[Table("H22fInlineListRows")]
public class H22fInlineListRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Stamp { get; set; }

    public List<DateTime> Marks { get; set; } = [];

    public DateTime[] Slots { get; set; } = [];
}

public class JsonInlineListContainsWithRegisteredListTypeTests
{
    private static readonly DateTime BaseStamp = new(2024, 3, 1, 10, 30, 0);

    [Fact]
    public void InlineDateListContainsADateColumnFiltersLikeLinq()
    {
        using TestDatabase db = Setup(nameof(InlineDateListContainsADateColumnFiltersLikeLinq));

        DateTime first = BaseStamp;
        DateTime third = BaseStamp.AddDays(2);

        List<int> expected = Rows()
            .Where(r => new List<DateTime> { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H22fInlineListRow>()
            .Where(r => new List<DateTime> { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineDateArrayContainsADateColumnFiltersLikeLinq()
    {
        using TestDatabase db = Setup(nameof(InlineDateArrayContainsADateColumnFiltersLikeLinq));

        DateTime first = BaseStamp;
        DateTime third = BaseStamp.AddDays(2);

        List<int> expected = Rows()
            .Where(r => new[] { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H22fInlineListRow>()
            .Where(r => new[] { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineDateListContainsADateColumnProjectsLikeLinq()
    {
        using TestDatabase db = Setup(nameof(InlineDateListContainsADateColumnProjectsLikeLinq));

        DateTime first = BaseStamp;
        DateTime third = BaseStamp.AddDays(2);

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new List<DateTime> { first, third }.Contains(r.Stamp)).ToList();
        List<bool> actual = db.Table<H22fInlineListRow>().OrderBy(r => r.Id)
            .Select(r => new List<DateTime> { first, third }.Contains(r.Stamp)).ToList();

        Assert.Equal([true, false, true], expected);
        Assert.Equal(expected, actual);
    }

    private static List<H22fInlineListRow> Rows()
    {
        return
        [
            new H22fInlineListRow { Id = 1, Stamp = BaseStamp, Marks = [BaseStamp], Slots = [BaseStamp] },
            new H22fInlineListRow { Id = 2, Stamp = BaseStamp.AddDays(1), Marks = [], Slots = [] },
            new H22fInlineListRow { Id = 3, Stamp = BaseStamp.AddDays(2), Marks = [BaseStamp.AddDays(2)], Slots = [] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H22fInlineListContext.Default), methodName);
        db.Table<H22fInlineListRow>().Schema.CreateTable();
        db.Table<H22fInlineListRow>().AddRange(Rows());
        return db;
    }
}
