using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20ArrToList")]
public class H20ArrToListRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class InlineArrayToListMethodTranslationTests
{
    private static List<H20ArrToListRow> Rows() =>
    [
        new H20ArrToListRow { Id = 1, A = 10 },
        new H20ArrToListRow { Id = 2, A = 20 },
        new H20ArrToListRow { Id = 3, A = 5 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20ArrToListRow>().Schema.CreateTable();
        db.Table<H20ArrToListRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ToListContainsInWhereMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => new[] { 10, 20 }.ToList().Contains(r.A))
            .Select(r => r.Id).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .Where(r => new[] { 10, 20 }.ToList().Contains(r.A))
            .Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToListIndexOfInWhereMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => new[] { 10, 20 }.ToList().IndexOf(r.A) >= 0)
            .Select(r => r.Id).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .Where(r => new[] { 10, 20 }.ToList().IndexOf(r.A) >= 0)
            .Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToListLastIndexOfInWhereMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => new[] { 10, 20 }.ToList().LastIndexOf(r.A) >= 0)
            .Select(r => r.Id).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .Where(r => new[] { 10, 20 }.ToList().LastIndexOf(r.A) >= 0)
            .Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToListIndexOfInSelectMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new[] { 10, 20 }.ToList().IndexOf(r.A)).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { 10, 20 }.ToList().IndexOf(r.A)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToListGetRangeInSelectMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new[] { r.A, 20 }.ToList().GetRange(0, 1)[0]).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r.A, 20 }.ToList().GetRange(0, 1)[0]).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToArrayAnyInWhereMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => new[] { 10, 20 }.ToArray().Any(v => v == r.A))
            .Select(r => r.Id).ToList();

        List<int> actual = db.Table<H20ArrToListRow>()
            .Where(r => new[] { 10, 20 }.ToArray().Any(v => v == r.A))
            .Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InstanceToArrayOverColumnListInitThrowsNotSupported()
    {
        using TestDatabase db = new(b => b.AddJsonContext(H20JsonOuterContext.Default), nameof(InstanceToArrayOverColumnListInitThrowsNotSupported));
        db.Table<H20JsonOuterRow>().Schema.CreateTable();
        db.Table<H20JsonOuterRow>().Add(new H20JsonOuterRow { Id = 1, Outer = 7, Plain = 9, Nums = [1, 2, 3] });

        Assert.Throws<NotSupportedException>(() => db.Table<H20JsonOuterRow>()
            .Where(r => new List<int> { r.Outer, 5 }.ToArray().Any(v => v == r.Plain))
            .Select(r => r.Id)
            .ToList());
    }
}
