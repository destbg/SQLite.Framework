using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BacktickChapters")]
public class BacktickChapterRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class MigrationFtsBacktickContentRenameTests
{
    [Fact]
    public void RenamingTheContentTableKeepsABacktickQuotedSearchTableReadable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OldBacktickChapters\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"OldBacktickChapters\" (\"Id\", \"Body\") VALUES (1, 'hello world')");
        db.Execute("CREATE VIRTUAL TABLE \"BacktickChapterSearch\" USING fts5(\"Body\", content=`OldBacktickChapters`, content_rowid=`Id`)");
        db.Execute("INSERT INTO \"BacktickChapterSearch\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"OldBacktickChapters\"");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<BacktickChapterRow>("OldBacktickChapters"))
            .Migrate();

        Assert.Equal(
            "hello world",
            db.ExecuteScalar<string>("SELECT \"Body\" FROM \"BacktickChapterSearch\" WHERE \"BacktickChapterSearch\" MATCH 'hello'"));
    }
}
