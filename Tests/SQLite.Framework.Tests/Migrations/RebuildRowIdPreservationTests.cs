using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SkuItem")]
public class SkuItemRow
{
    [Key]
    public string Sku { get; set; } = "";

    public int Qty { get; set; }
}

public class RebuildRowIdPreservationTests
{
    [Fact]
    public void RebuildKeepsRowIdsOfATableWithoutAnIntegerKey()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SkuItem\" (\"Sku\" TEXT NOT NULL PRIMARY KEY, \"Qty\" INTEGER NOT NULL, \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''))");
        db.Execute("INSERT INTO \"SkuItem\" (\"Sku\", \"Qty\") VALUES ('a', 1), ('b', 2), ('c', 3)");
        db.Execute("DELETE FROM \"SkuItem\" WHERE \"Sku\" = 'b'");
        long before = db.ExecuteScalar<long>("SELECT rowid FROM \"SkuItem\" WHERE \"Sku\" = 'c'");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SkuItemRow>(rebuild: true))
            .Migrate();

        long after = db.ExecuteScalar<long>("SELECT rowid FROM \"SkuItem\" WHERE \"Sku\" = 'c'");
        Assert.Equal(before, after);
    }
}
