using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigCaseBooks")]
public class MigCaseBook
{
    [Key, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[Table("MigCaseItems")]
public class MigCaseItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("MigCaseNotes")]
public class MigCaseNote
{
    [Key]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

public class MigrationTableNameCaseTests
{
    [Fact]
    public void RebuildMigratesDifferentCasedPhysicalTable()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"migcasebooks\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Title\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"migcasebooks\" (\"Title\") VALUES ('a')");
        db.Execute("INSERT INTO \"migcasebooks\" (\"Title\") VALUES ('b')");
        db.Execute("DELETE FROM \"migcasebooks\" WHERE \"Id\" = 2");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigCaseBook>(rebuild: true))
            .Migrate();

        MigCaseBook added = new() { Title = "c" };
        db.Table<MigCaseBook>().Add(added);

        Assert.Equal(3, added.Id);
        Assert.Equal(["a", "c"], db.Table<MigCaseBook>().OrderBy(b => b.Id).Select(b => b.Title).ToList());
    }

    [Fact]
    public void InPlaceMigrateReconcilesDifferentCasedPhysicalTable()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"migcaseitems\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"migcaseitems\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'kept', 'old')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigCaseItem>())
            .Migrate();

        Assert.False(db.Schema.ColumnExists<MigCaseItem>("Legacy"));
        Assert.Equal("kept", db.Table<MigCaseItem>().Single().Name);
    }

    [Fact]
    public void DropColumnStepMatchesDifferentCasedLiveColumn()
    {
        using TestDatabase db = new();
        db.Table<MigCaseNote>().Schema.CreateTable();
        db.Execute("ALTER TABLE \"MigCaseNotes\" ADD COLUMN \"Legacy\" TEXT");
        db.Table<MigCaseNote>().Add(new MigCaseNote { Id = 1, Text = "kept" });

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<MigCaseNote>("LEGACY"))
            .Migrate();

        Assert.False(db.Schema.ColumnExists<MigCaseNote>("Legacy"));
        Assert.Equal("kept", db.Table<MigCaseNote>().Single().Text);
    }
}
