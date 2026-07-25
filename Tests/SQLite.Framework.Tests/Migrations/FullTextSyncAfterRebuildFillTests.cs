using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FtsFillDoc")]
public class FtsFillDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(FtsFillDoc), AutoSync = FtsAutoSync.Triggers)]
[Table("FtsFillSearch")]
public class FtsFillSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FullTextSyncAfterRebuildFillTests
{
    [Fact]
    public void RebuildFillFromLegacyColumnKeepsSearchInSync()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"FtsFillDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL, \"OldBody\" TEXT)");
        db.Table<FtsFillSearch>().Schema.CreateTable();
        db.Execute("INSERT INTO \"FtsFillDoc\" (\"Id\", \"Body\", \"OldBody\") VALUES (1, 'placeholder one', 'green meadow'), (2, 'placeholder two', 'quiet river')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FtsFillDoc>(s => s.Set(d => d.Body, d => SQLiteColumn.Of<string>(d, "OldBody"))))
            .Migrate();

        long expectedMeadow;
        long expectedPlaceholder;
        using (TestDatabase fresh = new())
        {
            fresh.Table<FtsFillDoc>().Schema.CreateTable();
            fresh.Table<FtsFillSearch>().Schema.CreateTable();
            foreach (FtsFillDoc doc in db.Table<FtsFillDoc>().OrderBy(d => d.Id).ToList())
            {
                fresh.Table<FtsFillDoc>().Add(doc);
            }

            expectedMeadow = fresh.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsFillSearch\" WHERE \"FtsFillSearch\" MATCH 'meadow'");
            expectedPlaceholder = fresh.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsFillSearch\" WHERE \"FtsFillSearch\" MATCH 'placeholder'");
        }

        long actualMeadow = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsFillSearch\" WHERE \"FtsFillSearch\" MATCH 'meadow'");
        long actualPlaceholder = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsFillSearch\" WHERE \"FtsFillSearch\" MATCH 'placeholder'");
        Assert.Equal(expectedMeadow, actualMeadow);
        Assert.Equal(expectedPlaceholder, actualPlaceholder);
    }
}
