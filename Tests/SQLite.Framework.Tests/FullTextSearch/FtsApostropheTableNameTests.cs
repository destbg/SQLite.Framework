using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecQAposNotes")]
public class SecQAposNote
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SecQAposNote), AutoSync = FtsAutoSync.Triggers)]
[Table("SecQO'BrienSearch")]
public class SecQAposSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FtsApostropheTableNameTests
{
    [Fact]
    public void DropTableByNameDropsAnApostropheNamedSearchTableWithItsSyncTriggers()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SecQAposNote>();
        db.Schema.CreateTable<SecQAposSearch>();

        db.Schema.DropTable("SecQO'BrienSearch");

        Assert.False(db.Schema.TableExists("SecQO'BrienSearch"));
        Assert.Equal(0L, db.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND sql LIKE '%INTO \"SecQO''BrienSearch\"(%'"));
    }

    [Fact]
    public void RenameTableKeepsSyncTriggersWorkingOnAnApostropheNamedSearchTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SecQAposNote>();
        db.Schema.CreateTable<SecQAposSearch>();
        db.Table<SecQAposNote>().Add(new SecQAposNote { Id = 1, Body = "alpha" });

        db.Schema.RenameTable<SecQAposSearch>("SecQPlainSearch");
        db.Table<SecQAposNote>().Add(new SecQAposNote { Id = 2, Body = "beta" });

        Assert.True(db.Schema.TableExists("SecQPlainSearch"));
        Assert.Equal(3L, db.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND sql LIKE '%INTO \"SecQPlainSearch\"(%'"));
        Assert.Equal(2L, db.ExecuteScalar<long>(
            "SELECT rowid FROM \"SecQPlainSearch\" WHERE \"SecQPlainSearch\" MATCH 'beta'"));
    }
}
