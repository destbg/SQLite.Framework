using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RowidNamedItem")]
public class RowidNamedItem
{
    [Key]
    public string Code { get; set; } = "";

    [Column("rowid")]
    public string? LegacyPointer { get; set; }
}

[Table("RowidNamedParent")]
public class RowidNamedParent
{
    [Key]
    public string Code { get; set; } = "";
}

public class RowidNamedColumnRebuildTests
{
    [Fact]
    public void RebuildKeepsRealRowIdsOfATableWithARowidNamedColumn()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"RowidNamedItem\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"rowid\" TEXT, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"RowidNamedItem\" (\"Code\", \"rowid\", \"Old\") VALUES ('a', 'pa', 'x'), ('b', 'pb', 'y'), ('c', 'pc', 'z')");
        db.Execute("DELETE FROM \"RowidNamedItem\" WHERE \"Code\" = 'b'");
        List<long> before = db.Query<long>("SELECT _rowid_ FROM \"RowidNamedItem\" ORDER BY \"Code\"").ToList();

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidNamedItem>(rebuild: true))
            .Migrate();

        List<long> after = db.Query<long>("SELECT _rowid_ FROM \"RowidNamedItem\" ORDER BY \"Code\"").ToList();
        Assert.Equal(before, after);
    }

    [Fact]
    public void ParentRebuildKeepsRealRowIdsOfAChildWithARowidNamedColumn()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"RowidNamedParent\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Old\" TEXT)");
        db.Execute("CREATE TABLE \"RowidNamedKid\" (\"Key\" TEXT NOT NULL PRIMARY KEY, \"ParentCode\" TEXT REFERENCES \"RowidNamedParent\"(\"Code\"), \"rowid\" TEXT)");
        db.Execute("INSERT INTO \"RowidNamedParent\" (\"Code\", \"Old\") VALUES ('p', 'x')");
        db.Execute("INSERT INTO \"RowidNamedKid\" (\"Key\", \"ParentCode\", \"rowid\") VALUES ('a', 'p', 'ka'), ('b', 'p', 'kb'), ('c', 'p', 'kc')");
        db.Execute("DELETE FROM \"RowidNamedKid\" WHERE \"Key\" = 'b'");
        List<long> before = db.Query<long>("SELECT _rowid_ FROM \"RowidNamedKid\" ORDER BY \"Key\"").ToList();

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RowidNamedParent>(rebuild: true))
            .Migrate();

        List<long> after = db.Query<long>("SELECT _rowid_ FROM \"RowidNamedKid\" ORDER BY \"Key\"").ToList();
        Assert.Equal(before, after);
    }
}
