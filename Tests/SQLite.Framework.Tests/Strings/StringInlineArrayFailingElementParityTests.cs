using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22iFailingElemRows")]
public class H22iFailingElemRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22iFailingElemFns
{
    public static string Refuse(string reason)
    {
        throw new InvalidOperationException(reason);
    }
}

public class StringInlineArrayFailingElementParityTests
{
    [Fact]
    public void ConcatArrayWithAFailingArgumentTakingElementSurfacesTheMethodException()
    {
        using TestDatabase db = Setup();
        List<H22iFailingElemRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "x", H22iFailingElemFns.Refuse("no") }))
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H22iFailingElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "x", H22iFailingElemFns.Refuse("no") }))
            .ToList());
    }

    [Fact]
    public void JoinArrayWithAFailingArgumentTakingElementSurfacesTheMethodException()
    {
        using TestDatabase db = Setup();
        List<H22iFailingElemRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "x", H22iFailingElemFns.Refuse("no") }))
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H22iFailingElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "x", H22iFailingElemFns.Refuse("no") }))
            .ToList());
    }

    private static List<H22iFailingElemRow> Rows()
    {
        return
        [
            new H22iFailingElemRow { Id = 1, Name = "a" },
            new H22iFailingElemRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22iFailingElemRow>().Schema.CreateTable();
        db.Table<H22iFailingElemRow>().AddRange(Rows());
        return db;
    }
}
