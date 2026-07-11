using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig2_SweepRawReconcile")]
public class Mig2SweepRawReconcileRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("mig2_SweepRenameTarget")]
public class Mig2SweepRenameTargetRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("mig2_SweepExpand")]
public class Mig2SweepExpandRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

[Table("mig2_SweepRawFills")]
public class Mig2SweepRawFillRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationRawStepSchemaOrderingParityTests
{
    private static string TablesOf(SQLiteDatabase db)
    {
        return string.Join(",", db.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name"));
    }

    [Fact]
    public void RawCreateInEarlyVersionThenReconcileInLaterVersion()
    {
        Exception? collapsedEx;
        using (TestDatabase db = new(useFile: true))
        {
            collapsedEx = Record.Exception(() => db.Schema.Migrations()
                .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRawReconcile\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
                .Version(3, m => m.TableChanged<Mig2SweepRawReconcileRow>())
                .Migrate());
        }

        Exception? stepwiseEx;
        using (TestDatabase db = new(useFile: true))
        {
            stepwiseEx = Record.Exception(() =>
            {
                db.Schema.Migrations()
                    .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRawReconcile\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
                    .Migrate();
                db.Schema.Migrations()
                    .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRawReconcile\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
                    .Version(3, m => m.TableChanged<Mig2SweepRawReconcileRow>())
                    .Migrate();
            });
        }

        Assert.Equal(stepwiseEx?.Message, collapsedEx?.Message);
    }

    [Fact]
    public void RenameTableOfRawCreatedTableAcrossVersions()
    {
        string collapsed;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRenameSource\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)"))
                .Version(3, m => m.RenameTable<Mig2SweepRenameTargetRow>("mig2_SweepRenameSource").TableChanged<Mig2SweepRenameTargetRow>())
                .Migrate();
            collapsed = TablesOf(db);
        }

        string stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRenameSource\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)"))
                .Migrate();
            db.Schema.Migrations()
                .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRenameSource\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)"))
                .Version(3, m => m.RenameTable<Mig2SweepRenameTargetRow>("mig2_SweepRenameSource").TableChanged<Mig2SweepRenameTargetRow>())
                .Migrate();
            stepwise = TablesOf(db);
        }

        Assert.Equal(stepwise, collapsed);
    }

    [Fact]
    public void RenameColumnOfRawCreatedTableAcrossVersions()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(2, m => m.Sql("CREATE TABLE \"mig2_SweepRawReconcile\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT)"))
            .Version(3, m => m.Sql("INSERT INTO \"mig2_SweepRawReconcile\" (\"Id\", \"Title\") VALUES (1, 'kept')"))
            .Version(4, m => m.RenameColumn<Mig2SweepRawReconcileRow>("Title", "Name").TableChanged<Mig2SweepRawReconcileRow>())
            .Migrate();

        Mig2SweepRawReconcileRow row = db.Table<Mig2SweepRawReconcileRow>().Single();

        Assert.Equal(1, row.Id);
        Assert.Equal("kept", row.Name);
    }

    [Fact]
    public void RawCreatedTableWithFillsAcrossVersionsMatchesStepwise()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"mig2_SweepExpand\" (\"Id\" INTEGER PRIMARY KEY, \"A\" INTEGER NOT NULL, \"B\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"mig2_SweepExpand\" (\"Id\", \"A\", \"B\") VALUES (1, 10, 0)");
        db.Pragmas.UserVersion = 1;
        db.Schema.Migrations()
            .Version(2, m => m
                .Sql("CREATE TABLE \"mig2_SweepRawFills\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)")
                .Sql("INSERT INTO \"mig2_SweepRawFills\" (\"Id\", \"Name\") VALUES (1, 'raw')"))
            .Version(3, m => m.TableChanged<Mig2SweepRawFillRow>(s => s.Set(x => x.Name, "third")))
            .Version(4, m => m.TableChanged<Mig2SweepExpandRow>(s => s.Set(x => x.B, 44)))
            .Version(5, m => m.TableChanged<Mig2SweepRawFillRow>(s => s.Set(x => x.Name, "fifth")))
            .Migrate();

        Assert.Equal(44, db.Table<Mig2SweepExpandRow>().Single().B);
        Assert.Equal("fifth", db.Table<Mig2SweepRawFillRow>().Single().Name);
    }

    [Fact]
    public void TwoRawCreatedTablesReconcileInVersionOrder()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(2, m => m
                .Sql("CREATE TABLE \"mig2_SweepRawReconcile\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)")
                .Sql("CREATE TABLE \"mig2_SweepRawFills\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)")
                .Sql("INSERT INTO \"mig2_SweepRawReconcile\" (\"Id\", \"Name\") VALUES (1, 'one')")
                .Sql("INSERT INTO \"mig2_SweepRawFills\" (\"Id\", \"Name\") VALUES (2, 'two')"))
            .Version(3, m => m.TableChanged<Mig2SweepRawFillRow>(s => s.Set(x => x.Name, "third")))
            .Version(4, m => m.TableChanged<Mig2SweepRawReconcileRow>(s => s.Set(x => x.Name, "fourth")))
            .Migrate();

        Assert.Equal("fourth", db.Table<Mig2SweepRawReconcileRow>().Single().Name);
        Assert.Equal("third", db.Table<Mig2SweepRawFillRow>().Single().Name);
    }

    [Fact]
    public void TableCreatedAfterAnEarlierRawStepReconcilesInPlace()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(2, m => m.Sql("SELECT 1"))
            .Version(3, m => m
                .CreateTable<Mig2SweepRawFillRow>()
                .TableChanged<Mig2SweepRawFillRow>()
                .Sql("INSERT INTO \"mig2_SweepRawFills\" (\"Id\", \"Name\") VALUES (7, 'kept')"))
            .Migrate();

        Mig2SweepRawFillRow row = db.Table<Mig2SweepRawFillRow>().Single();

        Assert.Equal(7, row.Id);
        Assert.Equal("kept", row.Name);
    }

    [Fact]
    public void ExpandContractAcrossVersionsWithValues()
    {
        List<(int, int, int)> collapsed;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_SweepExpand\" (\"Id\" INTEGER PRIMARY KEY, \"A\" INTEGER NOT NULL)");
            db.Execute("INSERT INTO \"mig2_SweepExpand\" (\"Id\", \"A\") VALUES (1, 10)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.TableChanged<Mig2SweepExpandRow>(s => s.Set(r => r.B, r => r.A * 2)))
                .Version(3, m => m.Update<Mig2SweepExpandRow>(x => x.Id == 1, s => s.Set(r => r.A, 5)))
                .Migrate();
            collapsed = db.Table<Mig2SweepExpandRow>().OrderBy(r => r.Id)
                .ToList().Select(r => (r.Id, r.A, r.B)).ToList();
        }

        List<(int, int, int)> stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_SweepExpand\" (\"Id\" INTEGER PRIMARY KEY, \"A\" INTEGER NOT NULL)");
            db.Execute("INSERT INTO \"mig2_SweepExpand\" (\"Id\", \"A\") VALUES (1, 10)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.TableChanged<Mig2SweepExpandRow>(s => s.Set(r => r.B, r => r.A * 2)))
                .Migrate();
            db.Schema.Migrations()
                .Version(2, m => m.TableChanged<Mig2SweepExpandRow>(s => s.Set(r => r.B, r => r.A * 2)))
                .Version(3, m => m.Update<Mig2SweepExpandRow>(x => x.Id == 1, s => s.Set(r => r.A, 5)))
                .Migrate();
            stepwise = db.Table<Mig2SweepExpandRow>().OrderBy(r => r.Id)
                .ToList().Select(r => (r.Id, r.A, r.B)).ToList();
        }

        Assert.Equal(stepwise, collapsed);
    }
}
