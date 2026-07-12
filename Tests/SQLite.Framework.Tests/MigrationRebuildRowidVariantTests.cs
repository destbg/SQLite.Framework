using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RowidTrioRows")]
public class RowidTrioRow
{
    [Key]
    public string Code { get; set; } = "";

    [Column("rowid")]
    public int RowidValue { get; set; }

    [Column("_rowid_")]
    public int UnderscoreValue { get; set; }

    [Column("oid")]
    public int OidValue { get; set; }

    public string? Note { get; set; }
}

[Table("RowidTrioOther")]
public class RowidTrioOther
{
    [Key]
    public int Id { get; set; }
}

public class MigrationRebuildRowidVariantTests
{
    [Fact]
    public void RebuildTableWithAllRowidNamesShadowedKeepsRows()
    {
        using TestDatabase db = new();
        db.Table<RowidTrioRow>().Schema.CreateTable();
        db.Table<RowidTrioRow>().Add(new RowidTrioRow { Code = "a", RowidValue = 1, UnderscoreValue = 2, OidValue = 3, Note = "n" });

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidTrioRow>(rebuild: true))
            .Migrate();

        RowidTrioRow row = db.Table<RowidTrioRow>().Single();
        Assert.Equal("a", row.Code);
        Assert.Equal(1, row.RowidValue);
        Assert.Equal(2, row.UnderscoreValue);
        Assert.Equal(3, row.OidValue);
        Assert.Equal("n", row.Note);
    }

    [Fact]
    public void ReconcileAfterNamedDropOfSameTableRunsInDataPhase()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"RowidTrioOther\" (\"Id\" INTEGER PRIMARY KEY, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"RowidTrioOther\" (\"Id\", \"Legacy\") VALUES (7, 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("RowidTrioOther"))
            .Version(2, m => m.Sql("CREATE TABLE \"RowidTrioOther\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Version(3, m => m.TableChanged<RowidTrioOther>())
            .Migrate();

        Assert.Empty(db.Table<RowidTrioOther>().ToList());
    }

    [Fact]
    public void ReconcileWithUnrelatedEarlierDropStaysInSchemaPhase()
    {
        using TestDatabase db = new();
        db.Table<RowidTrioOther>().Schema.CreateTable();
        db.Execute("CREATE TABLE \"RowidTrioGone\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Table<RowidTrioOther>().Add(new RowidTrioOther { Id = 5 });

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("RowidTrioGone"))
            .Version(2, m => m.TableChanged<RowidTrioOther>())
            .Migrate();

        List<int> ids = db.Table<RowidTrioOther>().Select(r => r.Id).ToList();
        Assert.Equal([5], ids);
        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'RowidTrioGone'"));
    }
}
