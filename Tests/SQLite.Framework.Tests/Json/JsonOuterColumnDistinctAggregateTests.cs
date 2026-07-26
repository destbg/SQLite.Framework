using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class OuterColumnDistinctContext : JsonSerializerContext;

[Table("OuterColumnDistinctRows")]
public class OuterColumnDistinctRow
{
    [Key]
    public int Id { get; set; }

    public int Outer { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonOuterColumnDistinctAggregateTests
{
    [Fact]
    public void DistinctSumOfAnOuterColumnStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(DistinctSumOfAnOuterColumnStaysPerRow));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Sum())
            .ToList();

        List<int> actual = db.Table<OuterColumnDistinctRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Sum())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctAverageOfAnOuterColumnStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(DistinctAverageOfAnOuterColumnStaysPerRow));

        List<double> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Average())
            .ToList();

        List<double> actual = db.Table<OuterColumnDistinctRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Average())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctElementAtOfAnOuterColumnStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(DistinctElementAtOfAnOuterColumnStaysPerRow));
        NestedCorrelationFact.SkipIfUnsupported(db);

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().ElementAt(0))
            .ToList();

        List<int> actual = db.Table<OuterColumnDistinctRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().ElementAt(0))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctCountAfterASecondProjectionStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(DistinctCountAfterASecondProjectionStaysPerRow));
        NestedCorrelationFact.SkipIfUnsupported(db);

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Select(v => v * 2).Count())
            .ToList();

        List<int> actual = db.Table<OuterColumnDistinctRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.Select(n => r.Outer).Distinct().Select(v => v * 2).Count())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseAfterOrderingByAnOuterColumnKeepsElementOrder()
    {
        using TestDatabase db = Setup(nameof(ReverseAfterOrderingByAnOuterColumnKeepsElementOrder));
        NestedCorrelationFact.SkipIfUnsupported(db);

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => r.Nums.OrderBy(v => r.Outer).Distinct().Reverse().First())
            .ToList();

        List<int> actual = db.Table<OuterColumnDistinctRow>().OrderBy(r => r.Id)
            .Select(r => r.Nums.OrderBy(v => r.Outer).Distinct().Reverse().First())
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<OuterColumnDistinctRow> Rows()
    {
        return
        [
            new OuterColumnDistinctRow { Id = 1, Outer = 7, Nums = [1, 2, 3] },
            new OuterColumnDistinctRow { Id = 2, Outer = 8, Nums = [5] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(OuterColumnDistinctContext.Default), methodName);
        db.Table<OuterColumnDistinctRow>().Schema.CreateTable();
        db.Table<OuterColumnDistinctRow>().AddRange(Rows());
        return db;
    }
}

file static class NestedCorrelationFact
{
    public static void SkipIfUnsupported(TestDatabase db)
    {
        if (!Supports(db))
        {
            Assert.Skip($"SQLite {Version(db)} cannot read an outer column from a nested subquery.");
        }
    }

    private static bool Supports(TestDatabase db)
    {
        try
        {
            db.CreateCommand(
                    "SELECT (SELECT j.\"v\" FROM (SELECT o.\"Outer\" AS \"v\", MIN(k.\"key\") AS \"k\" " +
                    "FROM json_each(o.\"Nums\") k GROUP BY o.\"Outer\") j LIMIT 1) " +
                    "FROM \"OuterColumnDistinctRows\" AS o LIMIT 1", [])
                .ExecuteQuery<int>()
                .ToList();
            return true;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }

    private static string Version(TestDatabase db)
    {
        return db.CreateCommand("SELECT sqlite_version()", []).ExecuteQuery<string>().First();
    }
}
