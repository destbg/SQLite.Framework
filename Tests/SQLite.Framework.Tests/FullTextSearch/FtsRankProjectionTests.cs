using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FtsRankProjectionTests
{
    [Fact]
    public void RankInsideSelectProjectionReadsScore()
    {
        using TestDatabase db = new();
        db.Table<LftsSyncNote>().Schema.CreateTable();
        db.Table<LftsSyncNoteSearch>().Schema.CreateTable();
        db.Table<LftsSyncNote>().Add(new LftsSyncNote { Id = 1, Body = "apple pie recipe" });

        var hits = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .Select(s => new { s.Id, Score = SQLiteFTS5Functions.Rank(s) })
            .ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
        Assert.True(hits[0].Score < 0);
    }

    [Fact]
    public void RankInsideSelectProjectionWithWeightsReadsScore()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();
        db.Table<Article>().Add(new Article { Title = "apple pie", Body = "apple pie recipe", PublishedAt = DateTime.UtcNow });

        var hits = db.Table<ArticleSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .Select(s => new { s.Id, Score = SQLiteFTS5Functions.Rank(s) })
            .ToList();

        Assert.Single(hits);
        Assert.True(hits[0].Score < 0);
    }
}
