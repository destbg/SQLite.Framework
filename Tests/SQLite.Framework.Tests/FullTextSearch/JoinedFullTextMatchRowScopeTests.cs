using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26kMatchOwners")]
public class H26kMatchOwner
{
    [Key]
    public int Id { get; set; }

    public int DocId { get; set; }

    public string Name { get; set; } = "";
}

[Table("H26kMatchDocs")]
[FullTextSearch]
public class H26kMatchDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class JoinedFullTextMatchRowScopeTests
{
    [Fact]
    public void MatchOnAJoinedDocumentOrredWithAnotherPredicateKeepsTheDocumentScope()
    {
        using TestDatabase db = Setup(nameof(MatchOnAJoinedDocumentOrredWithAnotherPredicateKeepsTheDocumentScope));

        List<int> expected = Owners()
            .Join(Docs(), o => o.DocId, d => d.Id, (o, d) => new { o, d })
            .Where(x => x.d.Body.Split(' ').Contains("apple") || x.o.Name == "zed")
            .Select(x => x.o.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26kMatchOwner>()
            .Join(db.Table<H26kMatchDoc>(), o => o.DocId, d => d.Id, (o, d) => new { o, d })
            .Where(x => SQLiteFTS5Functions.Match(x.d, "apple") || x.o.Name == "zed")
            .Select(x => x.o.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedMatchOnAJoinedDocumentKeepsTheDocumentScope()
    {
        using TestDatabase db = Setup(nameof(NegatedMatchOnAJoinedDocumentKeepsTheDocumentScope));

        List<int> expected = Owners()
            .Join(Docs(), o => o.DocId, d => d.Id, (o, d) => new { o, d })
            .Where(x => !x.d.Body.Split(' ').Contains("apple"))
            .Select(x => x.o.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26kMatchOwner>()
            .Join(db.Table<H26kMatchDoc>(), o => o.DocId, d => d.Id, (o, d) => new { o, d })
            .Where(x => !SQLiteFTS5Functions.Match(x.d, "apple"))
            .Select(x => x.o.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26kMatchOwner> Owners()
    {
        return
        [
            new H26kMatchOwner { Id = 1, DocId = 3, Name = "zed" },
            new H26kMatchOwner { Id = 2, DocId = 1, Name = "alpha" },
            new H26kMatchOwner { Id = 3, DocId = 2, Name = "beta" }
        ];
    }

    private static List<H26kMatchDoc> Docs()
    {
        return
        [
            new H26kMatchDoc { Id = 1, Body = "apple" },
            new H26kMatchDoc { Id = 2, Body = "banana" },
            new H26kMatchDoc { Id = 3, Body = "cherry" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26kMatchOwner>().Schema.CreateTable();
        db.Table<H26kMatchDoc>().Schema.CreateTable();
        db.Table<H26kMatchOwner>().AddRange(Owners());
        db.Table<H26kMatchDoc>().AddRange(Docs());
        return db;
    }
}
