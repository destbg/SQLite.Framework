using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FtsRankProjectionVariantTests
{
    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<LftsSyncNote>().Schema.CreateTable();
        db.Table<LftsSyncNoteSearch>().Schema.CreateTable();
        db.Table<LftsSyncNote>().Add(new LftsSyncNote { Id = 1, Body = "apple pie recipe" });
        return db;
    }

    [Fact]
    public void RankAfterJoinReadsScore()
    {
        using TestDatabase db = Setup();

        var hits = (from s in db.Table<LftsSyncNoteSearch>()
            join n in db.Table<LftsSyncNote>() on s.Id equals n.Id
            where SQLiteFTS5Functions.Match(s, "apple")
            select new { n.Id, Score = SQLiteFTS5Functions.Rank(s) }).ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
        Assert.True(hits[0].Score < 0);
    }

    [Fact]
    public void RankAfterEntityCarryingSelectReadsScore()
    {
        using TestDatabase db = Setup();

        var hits = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .Select(s => new { S = s })
            .Select(x => new { Score = SQLiteFTS5Functions.Rank(x.S) })
            .ToList();

        Assert.Single(hits);
        Assert.True(hits[0].Score < 0);
    }

    [Fact]
    public void RankAfterDoubleEntityCarryReadsScore()
    {
        using TestDatabase db = Setup();

        var hits = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .Select(s => new { S = s })
            .Select(x => new { W = x })
            .Select(y => new { Score = SQLiteFTS5Functions.Rank(y.W.S) })
            .ToList();

        Assert.Single(hits);
        Assert.True(hits[0].Score < 0);
    }

    [Fact]
    public void RankOrderByOverCarriedEntityReadsRows()
    {
        using TestDatabase db = Setup();

        List<string> rows = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .Select(s => new { S = s, Tagged = CmcClientFns.Tag("q") })
            .OrderBy(x => SQLiteFTS5Functions.Rank(x.S))
            .Select(x => x.Tagged)
            .ToList();

        Assert.Equal(["[q]"], rows);
    }
}
