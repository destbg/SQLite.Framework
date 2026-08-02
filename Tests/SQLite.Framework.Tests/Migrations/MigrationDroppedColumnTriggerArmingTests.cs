using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigA_TrigDrop")]
public class MigATrigDropRow
{
    [Key]
    public int Id { get; set; }

    public string Keep { get; set; } = "";
}

public class MigrationDroppedColumnTriggerArmingTests
{
    [Fact]
    public void TableStaysWritableAfterReconcileDropsATriggerReferencedColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigA_TrigDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_migatrigdrop\" AFTER UPDATE ON \"MigA_TrigDrop\" BEGIN SELECT OLD.\"Gone\"; END");
        db.Execute("INSERT INTO \"MigA_TrigDrop\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");

        db.Schema.Migrations().Version(1, m => m.TableChanged<MigATrigDropRow>()).Migrate();

        Exception? ex = Record.Exception(() => db.Execute("UPDATE \"MigA_TrigDrop\" SET \"Keep\" = 'b' WHERE \"Id\" = 1"));
        Assert.Null(ex);
        Assert.Equal("b", db.Table<MigATrigDropRow>().Single().Keep);
    }

    [Fact]
    public void TableStaysWritableAfterReconcileDropsAnUnquotedTriggerReferencedColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigA_TrigDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_migatrigdrop2\" AFTER UPDATE ON \"MigA_TrigDrop\" BEGIN SELECT OLD.Gone; END");
        db.Execute("INSERT INTO \"MigA_TrigDrop\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");

        db.Schema.Migrations().Version(1, m => m.TableChanged<MigATrigDropRow>()).Migrate();

        Exception? ex = Record.Exception(() => db.Execute("UPDATE \"MigA_TrigDrop\" SET \"Keep\" = 'b' WHERE \"Id\" = 1"));
        Assert.Null(ex);
        Assert.Equal("b", db.Table<MigATrigDropRow>().Single().Keep);
    }
}
