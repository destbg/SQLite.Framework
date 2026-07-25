using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DropRefRow")]
public class DropRefRow
{
    [Key]
    public int Id { get; set; }

    public string Keep { get; set; } = "";
}

public class MigrationDropColumnTriggerReferenceTests
{
    [Fact]
    public void ReconcileDropsColumnReferencedByTrigger()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DropRefRow\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("INSERT INTO \"DropRefRow\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");
        db.Schema.CreateTrigger<DropRefRow>("trg_gone", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update,
            body: "SELECT RAISE(ABORT, 'no') WHERE OLD.\"Gone\" IS NOT NULL;");

        db.Schema.Migrations().Version(1, m => m.TableChanged<DropRefRow>()).Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<DropRefRow>(), c => c.Name == "Gone");
        Assert.Equal(1, db.Table<DropRefRow>().Count());
    }

    [Fact]
    public void DropColumnStepDropsColumnReferencedByTrigger()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DropRefRow\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("INSERT INTO \"DropRefRow\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");
        db.Schema.CreateTrigger<DropRefRow>("trg_gone", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update,
            body: "SELECT RAISE(ABORT, 'no') WHERE OLD.\"Gone\" IS NOT NULL;");

        db.Schema.Migrations().Version(1, m => m.DropColumn<DropRefRow>("Gone")).Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<DropRefRow>(), c => c.Name == "Gone");
        Assert.Equal(1, db.Table<DropRefRow>().Count());
    }

    [Fact]
    public void ReconcileDropsColumnReferencedByUnquotedTrigger()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DropRefRow\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("INSERT INTO \"DropRefRow\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");
        db.Schema.CreateTrigger<DropRefRow>("trg_gone_plain", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update,
            body: "SELECT RAISE(ABORT, 'no') WHERE OLD.Gone IS NOT NULL;");

        db.Schema.Migrations().Version(1, m => m.TableChanged<DropRefRow>()).Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<DropRefRow>(), c => c.Name == "Gone");
        Assert.Equal(1, db.Table<DropRefRow>().Count());
    }

    [Fact]
    public void ReconcileDropsColumnReferencedByIndex()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DropRefRow\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" TEXT NOT NULL, \"Gone\" TEXT)");
        db.Execute("INSERT INTO \"DropRefRow\" (\"Id\", \"Keep\", \"Gone\") VALUES (1, 'a', 'x')");
        db.Execute("CREATE INDEX \"ix_dropref_gone\" ON \"DropRefRow\" (\"Gone\")");

        db.Schema.Migrations().Version(1, m => m.TableChanged<DropRefRow>()).Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<DropRefRow>(), c => c.Name == "Gone");
        Assert.Equal(1, db.Table<DropRefRow>().Count());
    }
}
