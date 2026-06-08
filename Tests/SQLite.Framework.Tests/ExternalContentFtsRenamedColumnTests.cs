using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalContentFtsRenamedColumnTests
{
    [Fact]
    public void ExternalContentFts_RenamedSourceColumn_AutoSyncMatchesInsertedRow()
    {
        using TestDatabase db = new();
        db.Table<FtsRenamedSource>().Schema.CreateTable();
        db.Table<FtsRenamedSourceSearch>().Schema.CreateTable();

        db.Table<FtsRenamedSource>().Add(new FtsRenamedSource { Body = "hello world" });

        long matches = db.Table<FtsRenamedSourceSearch>()
            .LongCount(s => SQLiteFTS5Functions.Match(s, "hello"));

        Assert.Equal(1, matches);
    }
}

public class FtsRenamedSource
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Column("BodyText")]
    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(FtsRenamedSource), AutoSync = FtsAutoSync.Triggers)]
public class FtsRenamedSourceSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
