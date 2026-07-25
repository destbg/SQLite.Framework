using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class LjcAuthor
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class LjcBook
{
    [Key]
    public int Id { get; set; }

    public int? AuthorId { get; set; }

    public string Title { get; set; } = "";
}

public class LeftJoinClientEvalNullCheckParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LjcAuthor>().Schema.CreateTable();
        db.Table<LjcBook>().Schema.CreateTable();
        db.Table<LjcAuthor>().Add(new LjcAuthor { Id = 7, Name = "Ann" });
        db.Table<LjcBook>().Add(new LjcBook { Id = 1, AuthorId = 7, Title = "Matched" });
        db.Table<LjcBook>().Add(new LjcBook { Id = 2, AuthorId = 999, Title = "Unmatched" });
        return db;
    }

    private static (List<LjcAuthor> authors, List<LjcBook> books) Memory()
    {
        return (
            new List<LjcAuthor> { new() { Id = 7, Name = "Ann" } },
            new List<LjcBook>
            {
                new() { Id = 1, AuthorId = 7, Title = "Matched" },
                new() { Id = 2, AuthorId = 999, Title = "Unmatched" },
            });
    }

    [Fact]
    public void ClientEvalNullCheckTernary_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (List<LjcAuthor> authors, List<LjcBook> books) = Memory();

        List<string> oracle = (
            from b in books
            join a in authors on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(a == null ? "none" : a.Name)).ToList();

        List<string> actual = (
            from b in db.Table<LjcBook>()
            join a in db.Table<LjcAuthor>() on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(a == null ? "none" : a.Name)).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalAnonNullCheck_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (List<LjcAuthor> authors, List<LjcBook> books) = Memory();

        var oracle = (
            from b in books
            join a in authors on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select new { Name = InterceptorHelpers.Identity(a == null ? "none" : a.Name), Missing = a == null }).ToList();

        var actual = (
            from b in db.Table<LjcBook>()
            join a in db.Table<LjcAuthor>() on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select new { Name = InterceptorHelpers.Identity(a == null ? "none" : a.Name), Missing = a == null }).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalNullOnLeftEntityCheck_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (List<LjcAuthor> authors, List<LjcBook> books) = Memory();

        List<string> oracle = (
            from b in books
            join a in authors on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(null == a ? "none" : a.Name)).ToList();

        List<string> actual = (
            from b in db.Table<LjcBook>()
            join a in db.Table<LjcAuthor>() on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(null == a ? "none" : a.Name)).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalNotEqualNullEntityCheck_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (List<LjcAuthor> authors, List<LjcBook> books) = Memory();

        List<string> oracle = (
            from b in books
            join a in authors on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(a != null ? a.Name : "none")).ToList();

        List<string> actual = (
            from b in db.Table<LjcBook>()
            join a in db.Table<LjcAuthor>() on b.AuthorId equals a.Id into g
            from a in g.DefaultIfEmpty()
            orderby b.Id
            select InterceptorHelpers.Identity(a != null ? a.Name : "none")).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalScalarEqualityConstant_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (_, List<LjcBook> books) = Memory();

        List<string> oracle = books.OrderBy(b => b.Id)
            .Select(b => InterceptorHelpers.Identity(b.AuthorId == 7 ? "yes" : "no")).ToList();
        List<string> actual = db.Table<LjcBook>().OrderBy(b => b.Id)
            .Select(b => InterceptorHelpers.Identity(b.AuthorId == 7 ? "yes" : "no")).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalScalarNullCheck_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();
        (_, List<LjcBook> books) = Memory();

        List<string> oracle = books.OrderBy(b => b.Id)
            .Select(b => InterceptorHelpers.Identity(b.AuthorId == null ? "n" : "y")).ToList();
        List<string> actual = db.Table<LjcBook>().OrderBy(b => b.Id)
            .Select(b => InterceptorHelpers.Identity(b.AuthorId == null ? "n" : "y")).ToList();

        Assert.Equal(oracle, actual);
    }
}
