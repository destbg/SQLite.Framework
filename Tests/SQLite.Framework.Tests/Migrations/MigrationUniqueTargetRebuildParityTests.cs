using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig3_utr_parent")]
public sealed class UtrParent
{
    [Key]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
}

[Table("mig3_utr_child")]
public sealed class UtrChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(UtrParent), nameof(UtrParent.Code))]
    public string ParentCode { get; set; } = "";
}

public class MigrationUniqueTargetRebuildParityTests
{
    [Fact]
    public void ParentReferencedByUniqueColumnRebuildKeepsChild()
    {
        using TestDatabase db = new(useFile: true, methodName: "utr_one");
        db.Execute("CREATE TABLE \"mig3_utr_parent\" (\"Id\" INTEGER PRIMARY KEY, \"Code\" TEXT, \"Name\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE UNIQUE INDEX \"idx_mig3_utr_parent_Code\" ON \"mig3_utr_parent\" (\"Code\")");
        db.Execute("CREATE TABLE \"mig3_utr_child\" (\"Id\" INTEGER PRIMARY KEY, \"ParentCode\" TEXT REFERENCES \"mig3_utr_parent\"(\"Code\"))");
        db.Execute("INSERT INTO \"mig3_utr_parent\" (\"Id\", \"Code\", \"Name\", \"Legacy\") VALUES (1, 'AA', 'p', 'x')");
        db.Execute("INSERT INTO \"mig3_utr_child\" (\"Id\", \"ParentCode\") VALUES (10, 'AA'), (11, 'AA')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UtrParent>(rebuild: true))
            .Migrate();

        List<string> rows = db.Query<string>(
            "SELECT \"Id\" || '|' || \"ParentCode\" FROM \"mig3_utr_child\" ORDER BY \"Id\"").ToList();
        Assert.Equal(["10|AA", "11|AA"], rows);
    }
}
