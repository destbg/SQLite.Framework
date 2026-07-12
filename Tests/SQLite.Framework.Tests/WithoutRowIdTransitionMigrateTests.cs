using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[WithoutRowId]
[Table("RowidToWorItem")]
public class RowidToWorItem
{
    [Key]
    public string Code { get; set; } = "";

    public int Val { get; set; }
}

public class WithoutRowIdTransitionMigrateTests
{
    [Fact]
    public void RowidTableBecomesWithoutRowIdKeepingRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"RowidToWorItem\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"RowidToWorItem\" (\"Code\", \"Val\") VALUES ('a', 1), ('b', 2)");
        List<(string Code, int Val)> expected = [("a", 1), ("b", 2)];

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidToWorItem>())
            .Migrate();

        List<RowidToWorItem> rows = db.Table<RowidToWorItem>().OrderBy(r => r.Code).ToList();
        Assert.Equal(expected, rows.Select(r => (r.Code, r.Val)).ToList());
    }

    [Fact]
    public void RowidTableBecomesWithoutRowIdByExplicitRebuildKeepingRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"RowidToWorItem\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"RowidToWorItem\" (\"Code\", \"Val\") VALUES ('a', 1), ('b', 2)");
        List<(string Code, int Val)> expected = [("a", 1), ("b", 2)];

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidToWorItem>(rebuild: true))
            .Migrate();

        List<RowidToWorItem> rows = db.Table<RowidToWorItem>().OrderBy(r => r.Code).ToList();
        Assert.Equal(expected, rows.Select(r => (r.Code, r.Val)).ToList());
    }
}
