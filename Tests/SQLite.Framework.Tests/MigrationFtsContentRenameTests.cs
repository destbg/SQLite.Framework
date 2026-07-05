using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("Posts")]
public class RenamedPostRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(RenamedPostRow), AutoSync = FtsAutoSync.Triggers)]
[Table("PostSearch")]
public class RenamedPostSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationFtsContentRenameTests
{
    [Fact]
    public void RenamingTheContentTableKeepsTheSearchTableReadable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OldPosts\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"OldPosts\" (\"Id\", \"Body\") VALUES (1, 'hello world')");
        db.Execute("CREATE VIRTUAL TABLE \"PostSearch\" USING fts5(\"Body\", content=\"OldPosts\", content_rowid=\"Id\")");
        db.Execute("INSERT INTO \"PostSearch\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"OldPosts\"");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<RenamedPostRow>("OldPosts"))
            .Migrate();

        Assert.Equal(
            "hello world",
            db.ExecuteScalar<string>("SELECT \"Body\" FROM \"PostSearch\" WHERE \"PostSearch\" MATCH 'hello'"));
    }
}
