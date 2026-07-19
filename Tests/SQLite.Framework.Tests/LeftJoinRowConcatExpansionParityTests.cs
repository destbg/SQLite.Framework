using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20SgLjParent")]
public class H20SgLjParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H20SgLjChild")]
public class H20SgLjChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Tag { get; set; } = "";
}

public class LeftJoinRowConcatExpansionParityTests
{
    private static List<H20SgLjParent> Parents() =>
    [
        new H20SgLjParent { Id = 1, Name = "p1" },
        new H20SgLjParent { Id = 2, Name = "p2" },
    ];

    private static List<H20SgLjChild> Children() =>
    [
        new H20SgLjChild { Id = 5, ParentId = 1, Tag = "c1" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20SgLjParent>().Schema.CreateTable();
        db.Table<H20SgLjParent>().AddRange(Parents());
        db.Table<H20SgLjChild>().Schema.CreateTable();
        db.Table<H20SgLjChild>().AddRange(Children());
        return db;
    }

    private static string Describe(H20SgLjChild? c)
    {
        return c == null ? "none" : c.Id + ":" + c.Tag;
    }

    [Fact]
    public void StringConcatOverLeftJoinRowsMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H20SgLjParent> ps = Parents();
        List<H20SgLjChild> cs = Children();

        List<string> expected = (from p in ps
                join c in cs on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "-", c == null ? "null" : c.Tag))
            .ToList();

        List<string> actual = (from p in db.Table<H20SgLjParent>()
                join c in db.Table<H20SgLjChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "-", c == null ? "null" : c.Tag))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientMethodWithWholeRowArgMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H20SgLjParent> ps = Parents();
        List<H20SgLjChild> cs = Children();

        List<string> expected = (from p in ps
                join c in cs on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select Describe(c))
            .ToList();

        List<string> actual = (from p in db.Table<H20SgLjParent>()
                join c in db.Table<H20SgLjChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select Describe(c))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringConcatWholeRowArgMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H20SgLjParent> ps = Parents();
        List<H20SgLjChild> cs = Children();

        List<string> expected = (from p in ps
                join c in cs on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "|", c == null ? "null" : c))
            .ToList();

        List<string> actual = (from p in db.Table<H20SgLjParent>()
                join c in db.Table<H20SgLjChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "|", c == null ? "null" : c))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringConcatRowMembersInProjectionMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H20SgLjParent> ps = Parents();
        List<H20SgLjChild> cs = Children();

        List<string> expected = (from p in ps
                join c in cs on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select p.Name + "+" + (c == null ? "null" : c.Tag + c.Id))
            .ToList();

        List<string> actual = (from p in db.Table<H20SgLjParent>()
                join c in db.Table<H20SgLjChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select p.Name + "+" + (c == null ? "null" : c.Tag + c.Id))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
