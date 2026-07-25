using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20TsJoinParent")]
public class H20TsJoinParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H20TsJoinChild")]
public class H20TsJoinChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Tag { get; set; } = "";
}

[Table("H20TsStrFirstChild")]
public class H20TsStrFirstChild
{
    public string Code { get; set; } = "";

    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

public class JoinedEntityToStringProjectionTests
{
    private static TestDatabase Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children)
    {
        TestDatabase db = new();
        db.Table<H20TsJoinParent>().Schema.CreateTable();
        db.Table<H20TsJoinChild>().Schema.CreateTable();
        parents =
        [
            new H20TsJoinParent { Id = 1, Name = "p1" },
            new H20TsJoinParent { Id = 2, Name = "p2" },
        ];
        children = [new H20TsJoinChild { Id = 5, ParentId = 1, Tag = "t5" }];
        db.Table<H20TsJoinParent>().AddRange(parents);
        db.Table<H20TsJoinChild>().AddRange(children);
        return db;
    }

    [Fact]
    public void ToStringOverInnerJoinMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children);

        List<string?> expected = parents
            .Join(children, p => p.Id, c => c.ParentId, (p, c) => c.ToString())
            .ToList();

        List<string?> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsJoinChild>() on p.Id equals c.ParentId
                select c.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringOverLeftJoinMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children);

        List<string?> expected = parents
            .GroupJoin(children, p => p.Id, c => c.ParentId, (p, gc) => gc.FirstOrDefault())
            .Select(c => c == null ? "null" : c.ToString())
            .ToList();

        List<string?> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsJoinChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                select c == null ? "null" : c.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringOverInnerJoinConditionalMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children);

        List<string?> expected = parents
            .Join(children, p => p.Id, c => c.ParentId, (p, c) => c == null ? "null" : c.ToString())
            .ToList();

        List<string?> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsJoinChild>() on p.Id equals c.ParentId
                select c == null ? "null" : c.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringOverBareTableConditionalMatchesLinq()
    {
        using TestDatabase db = Seed(out _, out List<H20TsJoinChild> children);

        List<string?> expected = children
            .Select(c => c == null ? "null" : c.ToString())
            .ToList();

        List<string?> actual = db.Table<H20TsJoinChild>()
            .Select(c => c == null ? "null" : c.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NotNullOverLeftJoinMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children);

        List<string?> expected = parents
            .GroupJoin(children, p => p.Id, c => c.ParentId, (p, gc) => gc.FirstOrDefault())
            .Select(c => c != null ? c.ToString() : "null")
            .ToList();

        List<string?> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsJoinChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                select c != null ? c.ToString() : "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCheckValueOverLeftJoinMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20TsJoinParent> parents, out List<H20TsJoinChild> children);

        List<bool> expected = parents
            .GroupJoin(children, p => p.Id, c => c.ParentId, (p, gc) => gc.FirstOrDefault())
            .Select(c => c == null)
            .ToList();

        List<bool> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsJoinChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                select c == null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringFirstPropertyNullCheckMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<H20TsJoinParent>().Schema.CreateTable();
        db.Table<H20TsStrFirstChild>().Schema.CreateTable();
        List<H20TsJoinParent> parents =
        [
            new H20TsJoinParent { Id = 1, Name = "p1" },
            new H20TsJoinParent { Id = 2, Name = "p2" },
        ];
        List<H20TsStrFirstChild> children = [new H20TsStrFirstChild { Code = "c5", Id = 5, ParentId = 1 }];
        db.Table<H20TsJoinParent>().AddRange(parents);
        db.Table<H20TsStrFirstChild>().AddRange(children);

        List<string?> expected = parents
            .GroupJoin(children, p => p.Id, c => c.ParentId, (p, gc) => gc.FirstOrDefault())
            .Select(c => c == null ? "null" : c.ToString())
            .ToList();

        List<string?> actual = (from p in db.Table<H20TsJoinParent>()
                join c in db.Table<H20TsStrFirstChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                select c == null ? "null" : c.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
