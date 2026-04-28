using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FullTextSearchTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void Match_BindsQueryStringAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native aot"))
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("native aot", command.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   a0.Title AS "Title",
                   a0.Body AS "Body"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            """), N(command.CommandText));
    }

    [Fact]
    public void Match_ExpressionDsl_RendersFts5QueryString()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("native") && f.Prefix("aot")))
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("native AND aot*", command.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   a0.Title AS "Title",
                   a0.Body AS "Body"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            """), N(command.CommandText));
    }

    [Fact]
    public void Match_ColumnScopedString_RendersColumnPrefixInQuery()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a.Title, "native"))
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("{Title} : native", command.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   a0.Title AS "Title",
                   a0.Body AS "Body"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            """), N(command.CommandText));
    }

    [Fact]
    public void OrderByRank_WithCustomWeights_EmitsBm25Function()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .OrderBy(a => SQLiteFTS5Functions.Rank(a))
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   a0.Title AS "Title",
                   a0.Body AS "Body"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            ORDER BY bm25("ArticleSearch", 10, 1) ASC
            """), N(command.CommandText));
    }

    [Fact]
    public void OrderByRank_WithDefaultWeights_EmitsRankColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SimpleSearchEntity>();

        SQLiteCommand command = db.Table<SimpleSearchEntity>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .OrderBy(a => SQLiteFTS5Functions.Rank(a))
            .ToSqlCommand();

        Assert.Contains(".rank", N(command.CommandText));
        Assert.DoesNotContain("bm25(", N(command.CommandText));
    }

    [Fact]
    public void Snippet_EmitsSnippetAuxiliaryFunction()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .Select(a => new { a.Id, Snip = SQLiteFTS5Functions.Snippet(a, a.Body, "<b>", "</b>", "...", 8) })
            .ToSqlCommand();

        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   snippet("ArticleSearch", 1, @p1, @p2, @p3, @p4) AS "Snip"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            """), N(command.CommandText));
    }

    [Fact]
    public void Highlight_EmitsHighlightAuxiliaryFunction()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .Select(a => new { a.Id, Hl = SQLiteFTS5Functions.Highlight(a, a.Title, "[", "]") })
            .ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id",
                   highlight("ArticleSearch", 0, @p1, @p2) AS "Hl"
            FROM "ArticleSearch" AS a0
            WHERE "ArticleSearch" MATCH @p0
            """), N(command.CommandText));
    }

    [Fact]
    public void Match_TermThatLooksLikeKeyword_IsQuoted()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        db.Table<Article>().Add(new Article
        {
            Title = "Boolean operators AND OR NOT NEAR",
            Body = "AND clauses combine predicates.",
            PublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        List<ArticleSearch> hits = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("AND")))
            .ToList();

        Assert.Single(hits);
    }

    [Fact]
    public void Match_DynamicTermFromColumn_ResolvesPerRow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        db.Table<Article>().Add(new Article
        {
            Title = "aot",
            Body = "Building native AOT apps requires source generators.",
            PublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        var hits = (
                from s in db.Table<ArticleSearch>()
                join a in db.Table<Article>() on s.Id equals a.Id
                where SQLiteFTS5Functions.Match(s, f => f.Term(a.Title))
                select s.Id)
            .ToList();

        Assert.Single(hits);
    }

    [Fact]
    public void Match_DynamicTermFromColumn_EmitsPrintfSql()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand command = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Term(a.Title))
            select s.Id).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id"
            FROM "ArticleSearch" AS a0
            JOIN "Article" AS a1 ON a0.rowid = a1.Id
            WHERE "ArticleSearch" MATCH (printf('"%w"', a1.Title))
            """), N(command.CommandText));
    }

    [Fact]
    public void CreateTable_TokenizerWithSpecialChars_RoundTripsCorrectly()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CategorisedSearch>();

        db.CreateCommand("INSERT INTO CategorisedSearch(rowid, Body) VALUES (1, 'hello-world example_token 42')", []).ExecuteNonQuery();

        List<CategorisedSearch> hits = db.Table<CategorisedSearch>()
            .Where(c => SQLiteFTS5Functions.Match(c, f => f.Term("example_token")))
            .ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
    }

    [Fact]
    public void CreateTable_ExternalContent_CreatesVirtualTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        long count = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArticleSearch'");
        Assert.Equal(1, count);
    }

#if !SQLITECIPHER
    [Fact]
    public void CreateTable_InternalContentTrigram_CreatesVirtualTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<ArticleSearchInternal>();

        long count = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArticleSearchInternal'");
        Assert.Equal(1, count);
    }
#endif

    [Fact]
    public void Match_StringQuery_ReturnsMatchingRows()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .OrderBy(a => a.Id)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Title.Contains("Native"));
    }

    [Fact]
    public void Match_ExpressionAnd_ReturnsRowsMatchingBothTerms()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("native") && f.Term("aot")))
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void Match_ExpressionOr_ReturnsRowsMatchingEitherTerm()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("aot") || f.Term("trimmer")))
            .ToList();

        Assert.True(results.Count >= 2);
    }

    [Fact]
    public void Match_ExpressionPrefix_MatchesPrefix()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Prefix("nativ")))
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Match_ColumnScopedString_ReturnsRowsWhereColumnContainsTerm()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a.Title, "native"))
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void OrderBy_Rank_OrdersByRelevance()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        List<ArticleSearch> results = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .OrderBy(a => SQLiteFTS5Functions.Rank(a))
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void OrderBy_Rank_AppliesColumnWeights()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        db.Table<Article>().AddRange(new[]
        {
            new Article { Title = "Plain headline", Body = "kryptonite shows up only in the body.", PublishedAt = DateTime.UtcNow },
            new Article { Title = "kryptonite in headline", Body = "Body has nothing special here.", PublishedAt = DateTime.UtcNow },
        });

        List<int> orderedIds = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "kryptonite"))
            .OrderBy(a => SQLiteFTS5Functions.Rank(a))
            .Select(a => a.Id)
            .ToList();

        Assert.Equal(2, orderedIds.Count);
        int firstId = orderedIds[0];
        Article first = db.Table<Article>().Single(a => a.Id == firstId);
        Assert.Contains("kryptonite", first.Title);
    }

    [Fact]
    public void Snippet_ReturnsSnippetWithMarkers()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        var hits = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .Select(a => new { a.Id, Snip = SQLiteFTS5Functions.Snippet(a, a.Body, "<b>", "</b>", "...", 8) })
            .ToList();

        Assert.NotEmpty(hits);
        Assert.Contains(hits, h => h.Snip.Contains("<b>") && h.Snip.Contains("</b>"));
    }

    [Fact]
    public void Highlight_WrapsMatchingTokens()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        var hits = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "native"))
            .Select(a => new { a.Id, Hl = SQLiteFTS5Functions.Highlight(a, a.Title, "[", "]") })
            .ToList();

        Assert.NotEmpty(hits);
        Assert.Contains(hits, h => h.Hl.Contains('[') && h.Hl.Contains(']'));
    }

    [Fact]
    public void AutoSyncTriggers_PropagateInserts_ToFtsIndex()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        long ftsCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM ArticleSearch");
        long sourceCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM Article");
        Assert.Equal(sourceCount, ftsCount);
    }

    [Fact]
    public void AutoSyncTriggers_PropagateUpdates_ToFtsIndex()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        Article first = db.Table<Article>().OrderBy(a => a.Id).First();
        first.Body = "completely different content about apples and oranges";
        db.Table<Article>().Update(first);

        List<ArticleSearch> hits = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "apples"))
            .ToList();

        Assert.Single(hits);
        Assert.Equal(first.Id, hits[0].Id);
    }

    [Fact]
    public void AutoSyncTriggers_PropagateDeletes_ToFtsIndex()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        Article first = db.Table<Article>().OrderBy(a => a.Id).First();
        db.Table<Article>().Remove(first);

        long ftsCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM ArticleSearch");
        long sourceCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM Article");
        Assert.Equal(sourceCount, ftsCount);
    }

#if !SQLITECIPHER
    [Fact]
    public void Trigram_SubstringSearch_FindsRow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<ArticleSearchInternal>();
        db.CreateCommand("INSERT INTO ArticleSearchInternal(rowid, Code) VALUES (1, 'ExecuteUpdate')", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO ArticleSearchInternal(rowid, Code) VALUES (2, 'BatchInsert')", []).ExecuteNonQuery();

        List<ArticleSearchInternal> hits = db.Table<ArticleSearchInternal>()
            .Where(a => SQLiteFTS5Functions.Match(a, "ecuteUpd"))
            .ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
    }
#endif

    [Fact]
    public void Join_WithSourceTable_ReturnsContent()
    {
        using TestDatabase db = new();
        SeedArticles(db);

        var hits = (
                from s in db.Table<ArticleSearch>()
                join a in db.Table<Article>() on s.Id equals a.Id
                where SQLiteFTS5Functions.Match(s, "native")
                select new { s.Id, a.Title, a.PublishedAt }
            )
            .ToList();

        Assert.NotEmpty(hits);
        Assert.All(hits, h => Assert.NotEqual(default, h.PublishedAt));
    }

    private static void SeedArticles(TestDatabase db)
    {
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        DateTime now = new(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc);
        db.Table<Article>().AddRange(new[]
        {
            new Article { Title = "Native AOT in dotnet", Body = "Building native AOT apps requires source generators.", PublishedAt = now.AddDays(-3) },
            new Article { Title = "Trimmer warnings", Body = "The trimmer can drop methods used only via reflection. Native code generation helps.", PublishedAt = now.AddDays(-2) },
            new Article { Title = "Working with SQLite", Body = "FTS5 lets you full-text search rows quickly.", PublishedAt = now.AddDays(-1) },
        });
    }
}
