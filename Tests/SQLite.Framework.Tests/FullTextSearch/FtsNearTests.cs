using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
file sealed class FtsNearDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsNearTests
{
    private static readonly (int Id, string Body)[] Seed =
    [
        (1, "hello world"),
        (2, "hello there"),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<FtsNearDoc>().Schema.CreateTable();
        foreach ((int id, string body) in Seed)
        {
            db.Table<FtsNearDoc>().Add(new FtsNearDoc { Id = id, Body = body });
        }

        return db;
    }

    [Fact]
    public void Near_NoTerms_ThrowsClearError()
    {
        using TestDatabase db = CreateDb();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<FtsNearDoc>()
                .Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(2)))
                .ToList());

        Assert.Contains("Near", ex.Message);
    }

    [Fact]
    public void Near_SingleTerm_MatchesDocsWithTerm()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(d => d.Body.Split(' ').Contains("hello")).Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsNearDoc>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(2, "hello")))
            .Select(a => a.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Near_TwoTerms_MatchesDocsWithBothNearby()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed
            .Where(d => d.Body.Split(' ').Contains("hello") && d.Body.Split(' ').Contains("world"))
            .Select(d => d.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<FtsNearDoc>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(5, "hello", "world")))
            .Select(a => a.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
