using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CasedNote")]
public class CasedNoteRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

public class MigrationColumnNameCaseTests
{
    [Fact]
    public void ReconcileKeepsAColumnWhoseNameDiffersOnlyInCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CasedNote\" (\"Id\" INTEGER PRIMARY KEY, \"NOTE\" TEXT)");
        db.Execute("INSERT INTO \"CasedNote\" (\"Id\", \"NOTE\") VALUES (1, 'keep')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<CasedNoteRow>())
            .Migrate();

        Assert.Equal("keep", db.Table<CasedNoteRow>().Single().Note);
    }
}
