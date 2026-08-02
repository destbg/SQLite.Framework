using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigA_CaseTrig")]
public class MigACaseTrigRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

[Table("MigA_SelfCase")]
public class MigASelfCaseRow
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(MigASelfCaseRow))]
    public int? ParentId { get; set; }
}

public class MigrationRebuildNameCaseTests
{
    [Fact]
    public void RebuildKeepsATriggerOnADifferentCasedLiveTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"miga_casetrig\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_migacasenote\" AFTER INSERT ON \"miga_casetrig\" BEGIN SELECT NEW.\"Note\"; END");
        db.Execute("INSERT INTO \"miga_casetrig\" (\"Id\", \"Note\", \"Legacy\") VALUES (1, 'a', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigACaseTrigRow>())
            .Migrate();

        Assert.Equal("a", db.Table<MigACaseTrigRow>().Single().Note);
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_migacasenote'"));
    }

    [Fact]
    public void SelfReferencingRebuildOfADifferentCasedLiveTableKeepsRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"miga_selfcase\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"MigA_SelfCase\"(\"Id\"))");
        db.Execute("INSERT INTO \"miga_selfcase\" (\"Id\", \"ParentId\") VALUES (1, NULL), (2, 1)");
        db.Pragmas.ForeignKeys = true;

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigASelfCaseRow>(rebuild: true))
            .Migrate();

        List<string> rows = db.Query<string>(
            "SELECT \"Id\" || '|' || COALESCE(CAST(\"ParentId\" AS TEXT), 'null') FROM \"MigA_SelfCase\" ORDER BY \"Id\"").ToList();
        Assert.Equal(["1|null", "2|1"], rows);
    }
}
