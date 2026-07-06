using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RebuildCaseParent")]
public class RebuildCaseParentRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationRebuildChildReferenceCaseTests
{
    [Fact]
    public void ChildRowsSurviveWhenTheReferenceSpellsTheParentInDifferentCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RebuildCaseParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" INT)");
        db.Execute("CREATE TABLE \"RebuildCaseChild\" (\"Id\" INTEGER PRIMARY KEY, \"P\" INTEGER REFERENCES \"rebuildcaseparent\"(\"Id\") ON DELETE CASCADE)");
        db.Execute("INSERT INTO \"RebuildCaseParent\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'a', 0)");
        db.Execute("INSERT INTO \"RebuildCaseChild\" (\"Id\", \"P\") VALUES (10, 1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RebuildCaseParentRow>(rebuild: true))
            .Migrate();

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"RebuildCaseChild\""));
    }
}
