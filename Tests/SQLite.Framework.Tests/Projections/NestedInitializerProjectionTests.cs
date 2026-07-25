using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NestedInitSource
{
    [Key] public int Id { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

public class NestedInitSummary
{
    public decimal TotalPrice { get; set; }
}

public class NestedInitReport
{
    public NestedInitSummary Summary { get; set; } = new();
}

public class NestedInitReportWithField
{
    public NestedInitSummary Summary = new();
}

public class NestedInitListReport
{
    public List<string> Items { get; } = new();
    public int Score { get; set; }
}

public class NestedInitializerProjectionTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NestedInitSource>().Schema.CreateTable();
        db.Table<NestedInitSource>().Add(new NestedInitSource { Id = 1, Price = 9.99m, Name = "Alice", Score = 10 });
        return db;
    }

    [Fact]
    public void MemberMemberBinding_PopulatesNestedObject()
    {
        using TestDatabase db = Seed();
        List<decimal> expected = new List<NestedInitSource> { new() { Id = 1, Price = 9.99m, Name = "Alice", Score = 10 } }
            .OrderBy(p => p.Id).Select(p => new NestedInitReport { Summary = { TotalPrice = p.Price } }).Select(r => r.Summary.TotalPrice).ToList();
        List<decimal> actual = db.Table<NestedInitSource>().OrderBy(p => p.Id).Select(p => new NestedInitReport { Summary = { TotalPrice = p.Price } }).ToList().Select(r => r.Summary.TotalPrice).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberMemberBinding_OnField_LeavesNestedMemberAtDefault()
    {
        using TestDatabase db = Seed();
        List<decimal> actual = db.Table<NestedInitSource>().OrderBy(p => p.Id).Select(p => new NestedInitReportWithField { Summary = { TotalPrice = p.Price } }).ToList().Select(r => r.Summary.TotalPrice).ToList();
        Assert.Equal(new List<decimal> { 0m }, actual);
    }

    [Fact]
    public void MemberListBinding_WithConstantElements_PopulatesCollection()
    {
        using TestDatabase db = Seed();
        var expected = new List<NestedInitSource> { new() { Id = 1, Price = 9.99m, Name = "Alice", Score = 10 } }
            .OrderBy(p => p.Id).Select(p => new NestedInitListReport { Items = { "fixed" }, Score = p.Score }).Select(r => string.Join(",", r.Items) + ":" + r.Score).ToList();
        var actual = db.Table<NestedInitSource>().OrderBy(p => p.Id).Select(p => new NestedInitListReport { Items = { "fixed" }, Score = p.Score }).ToList().Select(r => string.Join(",", r.Items) + ":" + r.Score).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberListBinding_WithColumnElement_PopulatesCollection()
    {
        using TestDatabase db = Seed();
        var expected = new List<NestedInitSource> { new() { Id = 1, Price = 9.99m, Name = "Alice", Score = 10 } }
            .OrderBy(p => p.Id).Select(p => new NestedInitListReport { Items = { p.Name }, Score = p.Score }).Select(r => string.Join(",", r.Items) + ":" + r.Score).ToList();
        var actual = db.Table<NestedInitSource>().OrderBy(p => p.Id).Select(p => new NestedInitListReport { Items = { p.Name }, Score = p.Score }).ToList().Select(r => string.Join(",", r.Items) + ":" + r.Score).ToList();
        Assert.Equal(expected, actual);
    }
}
