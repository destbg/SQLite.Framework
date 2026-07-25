using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RenTblNew")]
public class RenameTableRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationRenameTableTests
{
    [Fact]
    public void ExistingTableIsRenamedAndDataKept()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RenTblOld\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"RenTblOld\" (\"Id\", \"Name\") VALUES (1, 'kept')");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RenameTableRow>())
            .Version(2, m => m.RenameTable<RenameTableRow>("RenTblOld").TableChanged<RenameTableRow>())
            .Migrate();

        Assert.False(db.Schema.TableExists("RenTblOld"));
        Assert.True(db.Schema.TableExists("RenTblNew"));
        Assert.Equal("kept", db.Table<RenameTableRow>().Single().Name);
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void RenameRunsBeforeCreateAcrossVersions()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RenTblOld\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"RenTblOld\" (\"Id\", \"Name\") VALUES (7, 'moved')");

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RenameTableRow>())
            .Version(2, m => m.RenameTable<RenameTableRow>("RenTblOld"))
            .Migrate();

        Assert.False(db.Schema.TableExists("RenTblOld"));
        Assert.Equal("moved", db.Table<RenameTableRow>().Single().Name);
    }

    [Fact]
    public void FreshDatabaseSkipsRenameAndCreatesCurrentName()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RenameTableRow>())
            .Version(2, m => m.RenameTable<RenameTableRow>("RenTblOld"))
            .Migrate();

        Assert.False(db.Schema.TableExists("RenTblOld"));
        Assert.True(db.Schema.TableExists("RenTblNew"));
        Assert.Empty(db.Table<RenameTableRow>().ToList());
    }

    [Fact]
    public void RenameToSameNameIsSkipped()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<RenameTableRow>().Schema.CreateTable();
        db.Table<RenameTableRow>().Add(new RenameTableRow { Id = 1, Name = "same" });

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<RenameTableRow>("RenTblNew"))
            .Migrate();

        Assert.Equal("same", db.Table<RenameTableRow>().Single().Name);
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void EmptyFromTableThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentException>(() => db.Schema.Migrations()
            .Version(1, m => m.RenameTable<RenameTableRow>(""))
            .Migrate());
    }

    [Fact]
    public void PlanDescribesRename()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m.RenameTable<RenameTableRow>("RenTblOld"))
            .Plan();

        Assert.Equal(["rename table \"RenTblOld\" to \"RenTblNew\""], plan.Operations);
    }
}
