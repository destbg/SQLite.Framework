using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FtsColumnScopeMultiTokenParityTests
{
    [Fact]
    public void StringColumnScope_MultipleTokens_ScopesEveryTokenToTheColumn()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();
        db.Table<Article>().Add(new Article { Title = "foo", Body = "bar", PublishedAt = new DateTime(2026, 1, 1) });
        db.Table<Article>().Add(new Article { Title = "foo bar", Body = "zzz", PublishedAt = new DateTime(2026, 1, 1) });

        List<int> oracle = db.Query<int>(
            "SELECT \"rowid\" FROM \"ArticleSearch\" WHERE \"ArticleSearch\" MATCH '{Title} : (foo bar)' ORDER BY \"rowid\"");
        List<int> actual = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a.Title, "foo bar"))
            .Select(a => a.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
