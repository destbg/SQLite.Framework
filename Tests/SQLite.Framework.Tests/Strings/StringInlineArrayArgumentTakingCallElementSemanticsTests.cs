using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22iTicketElemRows")]
public class H22iTicketElemRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22iTicketElemFns
{
    public static string Ticket(string prefix)
    {
        return prefix + Guid.NewGuid().ToString("N");
    }
}

public class StringInlineArrayArgumentTakingCallElementSemanticsTests
{
    [Fact]
    public void ConcatArrayWithAFreshValuePerRowElementVariesPerRow()
    {
        using TestDatabase db = Setup();
        List<H22iTicketElemRow> local = Rows();

        List<string> actual = db.Table<H22iTicketElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "n", H22iTicketElemFns.Ticket("t") }))
            .ToList();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { "n", H22iTicketElemFns.Ticket("t") }))
            .ToList();

        Assert.Equal(expected.Distinct().Count(), actual.Distinct().Count());
    }

    [Fact]
    public void JoinArrayWithAFreshValuePerRowElementVariesPerRow()
    {
        using TestDatabase db = Setup();
        List<H22iTicketElemRow> local = Rows();

        List<string> actual = db.Table<H22iTicketElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "n", H22iTicketElemFns.Ticket("t") }))
            .ToList();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { "n", H22iTicketElemFns.Ticket("t") }))
            .ToList();

        Assert.Equal(expected.Distinct().Count(), actual.Distinct().Count());
    }

    private static List<H22iTicketElemRow> Rows()
    {
        return
        [
            new H22iTicketElemRow { Id = 1, Name = "a" },
            new H22iTicketElemRow { Id = 2, Name = "b" },
            new H22iTicketElemRow { Id = 3, Name = "c" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22iTicketElemRow>().Schema.CreateTable();
        db.Table<H22iTicketElemRow>().AddRange(Rows());
        return db;
    }
}
