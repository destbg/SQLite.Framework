using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25rDisjunctionDocs")]
[FullTextSearch]
public class H25rDisjunctionDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Title { get; set; } = "";

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FullTextMatchInDisjunctionTests
{
    [Fact]
    public void MatchOrredWithAnotherPredicateReturnsTheRowsFromBothSides()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(d => HasToken(d, "apple") || d.Title == "banana")
            .Select(d => d.Id)
            .ToList();

        List<int> actual = db.Table<H25rDisjunctionDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, "apple") || d.Title == "banana")
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedMatchReturnsTheRowsThatDoNotMatch()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(d => !HasToken(d, "apple"))
            .Select(d => d.Id)
            .ToList();

        List<int> actual = db.Table<H25rDisjunctionDoc>()
            .Where(d => !SQLiteFTS5Functions.Match(d, "apple"))
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnMatchOrredWithAnotherPredicateReturnsTheRowsFromBothSides()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(d => d.Body.Split(' ').Contains("apple") || d.Title == "banana")
            .Select(d => d.Id)
            .ToList();

        List<int> actual = db.Table<H25rDisjunctionDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d.Body, "apple") || d.Title == "banana")
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25rDisjunctionDoc> Rows()
    {
        return
        [
            new H25rDisjunctionDoc { Id = 1, Title = "apple", Body = "pie" },
            new H25rDisjunctionDoc { Id = 2, Title = "banana", Body = "bread" },
            new H25rDisjunctionDoc { Id = 3, Title = "cherry", Body = "apple tart" }
        ];
    }

    private static bool HasToken(H25rDisjunctionDoc doc, string token)
    {
        return doc.Title.Split(' ').Contains(token) || doc.Body.Split(' ').Contains(token);
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H25rDisjunctionDoc>().Schema.CreateTable();
        db.Table<H25rDisjunctionDoc>().AddRange(Rows());
        return db;
    }
}
