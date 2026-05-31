using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FtsSrcItems")]
file sealed class FtsSrcItem
{
    [Key]
    [Column("Order")]
    public int Position { get; set; }
    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(FtsSrcItem), AutoSync = FtsAutoSync.Triggers)]
[Table("FtsSrcItemSearch")]
file sealed class FtsSrcItemSearch
{
    [FullTextRowId]
    public int Id { get; set; }
    [FullTextIndexed]
    public required string Body { get; set; }
}

public class Fts5ExternalContentTriggerBugTests
{
    [Fact]
    public void ExternalContentTriggerQuotesKeywordRowIdColumn()
    {
        using TestDatabase db = new();
        db.Table<FtsSrcItem>().Schema.CreateTable();
        db.Table<FtsSrcItemSearch>().Schema.CreateTable();

        Assert.True(db.Schema.TableExists("FtsSrcItemSearch"));
    }
}
