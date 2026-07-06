using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BracketChapters")]
public class BracketChapterRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class MigrationFtsBracketedContentRenameTests
{
    [Fact]
    public void RenamingTheContentTableKeepsABracketQuotedSearchTableReadable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OldBracketChapters\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"OldBracketChapters\" (\"Id\", \"Body\") VALUES (1, 'hello world')");
        db.Execute("CREATE VIRTUAL TABLE \"BracketChapterSearch\" USING fts5(\"Body\", content=[OldBracketChapters], content_rowid=[Id])");
        db.Execute("INSERT INTO \"BracketChapterSearch\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"OldBracketChapters\"");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<BracketChapterRow>("OldBracketChapters"))
            .Migrate();

        Assert.Equal(
            "hello world",
            db.ExecuteScalar<string>("SELECT \"Body\" FROM \"BracketChapterSearch\" WHERE \"BracketChapterSearch\" MATCH 'hello'"));
    }
}
