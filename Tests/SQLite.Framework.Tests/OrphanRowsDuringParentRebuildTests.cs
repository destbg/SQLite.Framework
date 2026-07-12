using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("OrphanHoldParent")]
public class OrphanHoldParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class OrphanRowsDuringParentRebuildTests
{
    [Fact]
    public void ParentRebuildWithOrphanedChildRowsThrows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"OrphanHoldParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"OrphanHoldKid\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"OrphanHoldParent\"(\"Id\"))");
        db.Execute("INSERT INTO \"OrphanHoldParent\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'p', 'x')");
        db.Execute("INSERT INTO \"OrphanHoldKid\" (\"Id\", \"ParentId\") VALUES (10, 1)");
        db.Pragmas.ForeignKeys = false;
        db.Execute("INSERT INTO \"OrphanHoldKid\" (\"Id\", \"ParentId\") VALUES (11, 99)");
        db.Pragmas.ForeignKeys = true;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.TableChanged<OrphanHoldParent>(rebuild: true))
            .Migrate());

        Assert.Equal(
            "Cannot rebuild table 'OrphanHoldParent'. Table 'OrphanHoldKid' has rows that violate its foreign key " +
            "on 'ParentId' to 'OrphanHoldParent'. Delete or fix these rows or turn foreign keys off for the migration.",
            ex.Message);
        List<string> rows = db.Query<string>("SELECT \"Id\" || '|' || \"ParentId\" FROM \"OrphanHoldKid\" ORDER BY \"Id\"").ToList();
        Assert.Equal(["10|1", "11|99"], rows);
    }

    [Fact]
    public void ParentRebuildWithForeignKeysOffKeepsOrphanedChildRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"OrphanHoldParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"OrphanHoldKid\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"OrphanHoldParent\"(\"Id\"))");
        db.Execute("INSERT INTO \"OrphanHoldParent\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'p', 'x')");
        db.Execute("INSERT INTO \"OrphanHoldKid\" (\"Id\", \"ParentId\") VALUES (10, 1)");
        db.Pragmas.ForeignKeys = false;
        db.Execute("INSERT INTO \"OrphanHoldKid\" (\"Id\", \"ParentId\") VALUES (11, 99)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<OrphanHoldParent>(rebuild: true))
            .Migrate();

        List<string> rows = db.Query<string>("SELECT \"Id\" || '|' || \"ParentId\" FROM \"OrphanHoldKid\" ORDER BY \"Id\"").ToList();
        Assert.Equal(["10|1", "11|99"], rows);
    }
}
