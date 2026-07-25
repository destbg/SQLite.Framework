using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RowidKind")]
public class RowidKindRow
{
    [Key]
    public string Kind { get; set; } = "";

    public string? Note { get; set; }
}

[WithoutRowId]
[Table("WithoutRowIdRebuild")]
public class WithoutRowIdRebuildRow
{
    [Key]
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
}

public class MigrationRebuildRowIdPreservationTests
{
    [Fact]
    public void RebuildKeepsRowIdWhenCreateSqlMentionsWithoutRowId()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RowidKind\" (\"Kind\" TEXT PRIMARY KEY, \"Note\" TEXT, CHECK (\"Kind\" <> 'WITHOUT ROWID'))");
        db.Execute("INSERT INTO \"RowidKind\" (\"Kind\", \"Note\") VALUES ('a', 'n1')");
        db.Execute("INSERT INTO \"RowidKind\" (\"Kind\", \"Note\") VALUES ('b', 'n2')");
        db.Execute("INSERT INTO \"RowidKind\" (\"Kind\", \"Note\") VALUES ('c', 'n3')");
        db.Execute("DELETE FROM \"RowidKind\" WHERE \"Kind\" = 'b'");

        List<long> before = db.Query<long>("SELECT rowid FROM \"RowidKind\" ORDER BY \"Kind\"");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidKindRow>(rebuild: true))
            .Migrate();

        List<long> after = db.Query<long>("SELECT rowid FROM \"RowidKind\" ORDER BY \"Kind\"");

        Assert.Equal(before, after);
    }

    [Fact]
    public void RebuildOfGenuineWithoutRowIdTableKeepsRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<WithoutRowIdRebuildRow>().Schema.CreateTable();
        db.Table<WithoutRowIdRebuildRow>().Add(new WithoutRowIdRebuildRow { Code = "a", Name = "n1" });
        db.Execute("ALTER TABLE \"WithoutRowIdRebuild\" ADD COLUMN \"Legacy\" TEXT");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<WithoutRowIdRebuildRow>(rebuild: true))
            .Migrate();

        Assert.Equal("n1", db.Table<WithoutRowIdRebuildRow>().Single().Name);
    }
}
