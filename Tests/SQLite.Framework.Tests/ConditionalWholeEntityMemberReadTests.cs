using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TernRow")]
public class TernRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class ConditionalWholeEntityMemberReadTests
{
    private static List<TernRow> Rows() =>
    [
        new TernRow { Id = 1, Name = "L1" },
        new TernRow { Id = 2, Name = null },
        new TernRow { Id = 3, Name = "L3" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<TernRow>().Schema.CreateTable();
        db.Table<TernRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConditionalBetweenTwoJoinedEntityRowsMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string?> expected = Rows()
            .Join(Rows(), a => a.Id, b => 4 - b.Id, (a, b) => new { a, b })
            .OrderBy(x => x.a.Id)
            .Select(x => (x.a.Id > 1 ? x.b : x.a).Name)
            .ToList();

        List<string?> actual = db.Table<TernRow>()
            .Join(db.Table<TernRow>(), a => a.Id, b => 4 - b.Id, (a, b) => new { a, b })
            .OrderBy(x => x.a.Id)
            .Select(x => (x.a.Id > 1 ? x.b : x.a).Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalEntityOrCapturedFallbackMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        TernRow fallback = new() { Id = 99, Name = "fb" };

        List<string?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id > 1 ? r : fallback).Name)
            .ToList();

        List<string?> actual = db.Table<TernRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id > 1 ? r : fallback).Name)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
