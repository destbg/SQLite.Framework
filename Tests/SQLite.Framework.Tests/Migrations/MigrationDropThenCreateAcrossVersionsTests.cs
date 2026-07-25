using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReintroducedLog")]
public class ReintroducedLogRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

[Table("OtherMigratedLog")]
public class OtherMigratedRow
{
    [Key]
    public int Id { get; set; }
}

public class MigrationDropThenCreateAcrossVersionsTests
{
    [Fact]
    public void StepwiseUpgradeEndsWithTheReintroducedTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ReintroducedLog\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Schema.Migrations()
            .Version(1, m => m.DropTable("ReintroducedLog"))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("ReintroducedLog"))
            .Version(2, m => m.CreateTable<ReintroducedLogRow>())
            .Migrate();

        Assert.Equal(2, db.Pragmas.UserVersion);
        Assert.True(db.Schema.TableExists("ReintroducedLog"));
    }

    [Fact]
    public void FreshDatabaseEndsWithTheReintroducedTable()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("ReintroducedLog"))
            .Version(2, m => m.CreateTable<ReintroducedLogRow>())
            .Migrate();

        Assert.Equal(2, db.Pragmas.UserVersion);
        Assert.True(db.Schema.TableExists("ReintroducedLog"));
    }

    [Fact]
    public void DropStaysWhenALaterVersionCreatesAnotherTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ReintroducedLog\" (\"Id\" INTEGER PRIMARY KEY)");

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("ReintroducedLog"))
            .Version(2, m => m.CreateTable<OtherMigratedRow>())
            .Migrate();

        Assert.False(db.Schema.TableExists("ReintroducedLog"));
        Assert.True(db.Schema.TableExists("OtherMigratedLog"));
    }

    [Fact]
    public void FreshDatabaseInsertsIntoTheReintroducedTable()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("ReintroducedLog"))
            .Version(2, m => m
                .CreateTable<ReintroducedLogRow>()
                .Insert(new ReintroducedLogRow { Id = 1, Note = "seed" }))
            .Migrate();

        Assert.Equal("seed", db.Table<ReintroducedLogRow>().Single().Note);
    }
}
