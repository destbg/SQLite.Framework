using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21dInvokedElemRows")]
public class H21dInvokedElemRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21dInvokedElemFns
{
    public static string Boom()
    {
        throw new InvalidOperationException("boom");
    }
}

public class StringInlineArrayInvokedElementSemanticsTests
{
    private static List<H21dInvokedElemRow> Rows()
    {
        return
        [
            new H21dInvokedElemRow { Id = 1, Name = "a" },
            new H21dInvokedElemRow { Id = 2, Name = "b" },
            new H21dInvokedElemRow { Id = 3, Name = "c" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21dInvokedElemRow>().Schema.CreateTable();
        db.Table<H21dInvokedElemRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConcatArrayWithFailingElementSurfacesTheMethodException()
    {
        using TestDatabase db = Setup();
        List<H21dInvokedElemRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "x", H21dInvokedElemFns.Boom() }))
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H21dInvokedElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "x", H21dInvokedElemFns.Boom() }))
            .ToList());
    }

    [Fact]
    public void ConcatArrayWithNewGuidElementVariesPerRow()
    {
        using TestDatabase db = Setup();
        List<H21dInvokedElemRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { Guid.NewGuid() }))
            .ToList();

        List<string> actual = db.Table<H21dInvokedElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { Guid.NewGuid() }))
            .ToList();

        Assert.Equal(expected.Distinct().Count(), actual.Distinct().Count());
    }

    [Fact]
    public void JoinArrayWithNewGuidElementVariesPerRow()
    {
        using TestDatabase db = Setup();
        List<H21dInvokedElemRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "n", Guid.NewGuid() }))
            .ToList();

        List<string> actual = db.Table<H21dInvokedElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "n", Guid.NewGuid() }))
            .ToList();

        Assert.Equal(expected.Distinct().Count(), actual.Distinct().Count());
    }
}
