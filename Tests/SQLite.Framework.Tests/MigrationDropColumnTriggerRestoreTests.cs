using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TrigTrimBook")]
public class TrigTrimBookRow
{
    [Key]
    public int Id { get; set; }

    public string? Title { get; set; }
}

public class MigrationDropColumnTriggerRestoreTests
{
    [Fact]
    public void TableStaysWritableAfterDroppingATriggerReferencedColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"TrigTrimBook\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_trim_note\" AFTER UPDATE ON \"TrigTrimBook\" BEGIN SELECT OLD.\"Legacy\"; END");
        db.Execute("INSERT INTO \"TrigTrimBook\" (\"Id\", \"Title\", \"Legacy\") VALUES (1, 't', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<TrigTrimBookRow>("Legacy"))
            .Migrate();

        Exception? ex = Record.Exception(() => db.Execute("UPDATE \"TrigTrimBook\" SET \"Title\" = 'y'"));
        Assert.Null(ex);
    }
}
