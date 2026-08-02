using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnBArticleRows")]
public class ChnBArticleRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ChnBArticleRow), AutoSync = FtsAutoSync.Triggers)]
[Table("ChnBArticleSearch")]
public class ChnBArticleSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Title { get; set; } = "";
}

public class MigrationFtsContentColumnDropTests
{
    [Fact]
    public void ADeclaredSearchRebuildRecoversADroppedContentColumn()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(3, m => m.TableChanged<ChnBArticleRow>())
            .Version(4, m => m.TableChanged<ChnBArticleSearchRow>())
            .Version(5, m => m.RebuildFullTextSearch<ChnBArticleSearchRow>())
            .Migrate();

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBArticleSearch\" WHERE \"ChnBArticleSearch\" MATCH 'hello'"));

        db.Table<ChnBArticleRow>().Add(new ChnBArticleRow { Id = 2, Title = "fresh" });

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBArticleSearch\" WHERE \"ChnBArticleSearch\" MATCH 'fresh'"));
    }

    [Fact]
    public void DroppingAnIndexedContentColumnWithoutASearchRebuildStopsWithGuidance()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);
        db.Pragmas.UserVersion = 1;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(3, m => m.TableChanged<ChnBArticleRow>())
            .Migrate());

        Assert.Contains("ChnBArticleSearch", ex.Message);
        Assert.Contains("Body", ex.Message);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnBArticleRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Title\" TEXT NOT NULL, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ChnBArticleRows\" (\"Id\", \"Title\", \"Body\") VALUES (1, 'hello', 'world body')");
        db.Execute("CREATE VIRTUAL TABLE \"ChnBArticleSearch\" USING fts5(\"Title\", \"Body\", content='ChnBArticleRows', content_rowid='Id', tokenize='unicode61')");
        db.Execute("CREATE TRIGGER \"ChnBArticleSearch_sync_ai\" AFTER INSERT ON \"ChnBArticleRows\" BEGIN INSERT INTO \"ChnBArticleSearch\"(rowid, \"Title\", \"Body\") VALUES (new.\"Id\", new.\"Title\", new.\"Body\"); END");
        db.Execute("CREATE TRIGGER \"ChnBArticleSearch_sync_ad\" AFTER DELETE ON \"ChnBArticleRows\" BEGIN INSERT INTO \"ChnBArticleSearch\"(\"ChnBArticleSearch\", rowid, \"Title\", \"Body\") VALUES('delete', old.\"Id\", old.\"Title\", old.\"Body\"); END");
        db.Execute("CREATE TRIGGER \"ChnBArticleSearch_sync_au\" AFTER UPDATE ON \"ChnBArticleRows\" BEGIN INSERT INTO \"ChnBArticleSearch\"(\"ChnBArticleSearch\", rowid, \"Title\", \"Body\") VALUES('delete', old.\"Id\", old.\"Title\", old.\"Body\"); INSERT INTO \"ChnBArticleSearch\"(rowid, \"Title\", \"Body\") VALUES (new.\"Id\", new.\"Title\", new.\"Body\"); END");
        db.Execute("INSERT INTO \"ChnBArticleSearch\"(rowid, \"Title\", \"Body\") SELECT \"Id\", \"Title\", \"Body\" FROM \"ChnBArticleRows\"");
    }
}
