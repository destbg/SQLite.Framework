using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
file sealed class FtsMappedDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [System.ComponentModel.DataAnnotations.Schema.Column("title_text")]
    public required string Title { get; set; }

    [FullTextIndexed]
    [System.ComponentModel.DataAnnotations.Schema.Column("body_text")]
    public required string Body { get; set; }
}

public class FtsColumnScopeMappedNameTests
{
    private static readonly (int Id, string Title, string Body)[] Seed =
    [
        (1, "apple", "hello world"),
        (2, "hello", "banana"),
        (3, "cherry", "hello there"),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<FtsMappedDoc>().Schema.CreateTable();
        foreach ((int id, string title, string body) in Seed)
        {
            db.Table<FtsMappedDoc>().Add(new FtsMappedDoc { Id = id, Title = title, Body = body });
        }

        return db;
    }

    [Fact]
    public void ColumnScope_MappedBodyColumn_MatchesOnlyBody()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(d => d.Body.Split(' ').Contains("hello")).Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsMappedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, f => f.Column(d.Body, f.Term("hello"))))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnScope_MappedTitleColumn_MatchesOnlyTitle()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(d => d.Title.Split(' ').Contains("hello")).Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsMappedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, f => f.Column(d.Title, f.Term("hello"))))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnScope_MappedBodyColumn_TermInOtherColumn_NoMatch()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(d => d.Body.Split(' ').Contains("apple")).Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsMappedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, f => f.Column(d.Body, f.Term("apple"))))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Empty(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnScope_MappedBodyColumn_CompoundBody()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed
            .Where(d => d.Body.Split(' ').Contains("hello") && d.Body.Split(' ').Contains("world"))
            .Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsMappedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, f => f.Column(d.Body, f.Term("hello") && f.Term("world"))))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
