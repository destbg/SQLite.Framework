using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig3_srr_node")]
public sealed class SrrNode
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(SrrNode))]
    public int? ParentId { get; set; }

    public string Label { get; set; } = "";
}

public class MigrationSelfReferencingRebuildParityTests
{
    [Fact]
    public void SelfReferencingTableRebuildKeepsTree()
    {
        using TestDatabase db = new(useFile: true, methodName: "srr_one");
        db.Execute("CREATE TABLE \"mig3_srr_node\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"mig3_srr_node\"(\"Id\"), \"Label\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"mig3_srr_node\" (\"Id\", \"ParentId\", \"Label\", \"Legacy\") VALUES (1, NULL, 'root', 'x'), (2, 1, 'a', 'x'), (3, 1, 'b', 'x'), (4, 2, 'c', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SrrNode>(rebuild: true))
            .Migrate();

        List<string> rows = db.Query<string>(
            "SELECT \"Id\" || '|' || COALESCE(CAST(\"ParentId\" AS TEXT), 'null') || '|' || \"Label\" FROM \"mig3_srr_node\" ORDER BY \"Id\"").ToList();
        Assert.Equal(["1|null|root", "2|1|a", "3|1|b", "4|2|c"], rows);
    }

    [Fact]
    public void SelfReferencingRowPointingAtItselfSurvivesRebuild()
    {
        using TestDatabase db = new(useFile: true, methodName: "srr_two");
        db.Execute("CREATE TABLE \"mig3_srr_node\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"mig3_srr_node\"(\"Id\"), \"Label\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"mig3_srr_node\" (\"Id\", \"ParentId\", \"Label\", \"Legacy\") VALUES (5, 5, 'loop', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SrrNode>(rebuild: true))
            .Migrate();

        List<string> rows = db.Query<string>(
            "SELECT \"Id\" || '|' || CAST(\"ParentId\" AS TEXT) || '|' || \"Label\" FROM \"mig3_srr_node\"").ToList();
        Assert.Equal(["5|5|loop"], rows);
    }

    [Fact]
    public void SelfReferencingTableWithAnExternalChildSurvivesRebuild()
    {
        using TestDatabase db = new(useFile: true, methodName: "srr_three");
        db.Execute("CREATE TABLE \"mig3_srr_node\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"mig3_srr_node\"(\"Id\"), \"Label\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"mig3_srr_leaf\" (\"Id\" INTEGER PRIMARY KEY, \"NodeId\" INTEGER REFERENCES \"mig3_srr_node\"(\"Id\"))");
        db.Execute("INSERT INTO \"mig3_srr_node\" (\"Id\", \"ParentId\", \"Label\", \"Legacy\") VALUES (1, NULL, 'root', 'x'), (2, 1, 'a', 'x')");
        db.Execute("INSERT INTO \"mig3_srr_leaf\" (\"Id\", \"NodeId\") VALUES (10, 2)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SrrNode>(rebuild: true))
            .Migrate();

        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"mig3_srr_node\""));
        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT \"NodeId\" FROM \"mig3_srr_leaf\" WHERE \"Id\" = 10"));
    }
}
