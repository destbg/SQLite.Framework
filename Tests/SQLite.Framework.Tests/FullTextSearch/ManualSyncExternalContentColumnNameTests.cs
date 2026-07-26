using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22cManualArticle")]
public class H22cManualArticle
{
    [Key]
    public int Id { get; set; }

    public required string Headline { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H22cManualArticle), AutoSync = FtsAutoSync.Manual)]
[Table("H22cManualArticleSearch")]
public class H22cManualArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [Column("Headline")]
    public required string Text { get; set; }
}

public class ManualSyncExternalContentColumnNameTests
{
    [Fact]
    public void CreateTableEmitsTheContentColumnNameForARenamedIndexedProperty()
    {
        using TestDatabase db = new();
        db.Table<H22cManualArticle>().Schema.CreateTable();
        db.Table<H22cManualArticleSearch>().Schema.CreateTable();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "H22cManualArticleSearch" USING fts5("Headline", content='H22cManualArticle', content_rowid='Id', tokenize='unicode61 remove_diacritics 2')""",
            db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'H22cManualArticleSearch'"));
    }

    [Fact]
    public void MatchReadsBackTheRenamedIndexedPropertyFromTheContentTable()
    {
        using TestDatabase db = new();
        db.Table<H22cManualArticle>().Schema.CreateTable();
        db.Table<H22cManualArticleSearch>().Schema.CreateTable();

        List<H22cManualArticle> rows =
        [
            new H22cManualArticle { Id = 1, Headline = "native aot works" },
            new H22cManualArticle { Id = 2, Headline = "reflection is slow" },
        ];
        db.Table<H22cManualArticle>().AddRange(rows);
        db.Execute("INSERT INTO \"H22cManualArticleSearch\"(\"H22cManualArticleSearch\") VALUES('rebuild')");

        List<string> expected = rows
            .Where(r => r.Headline.Split(' ').Contains("native"))
            .Select(r => r.Headline)
            .ToList();
        List<string> actual = db.Table<H22cManualArticleSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "native"))
            .Select(s => s.Text)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
