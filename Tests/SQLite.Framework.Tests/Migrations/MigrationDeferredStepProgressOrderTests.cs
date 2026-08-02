using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigIRenamedTable")]
public class MigIRenamedTableRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

[Table("MigIDeferredCreated")]
public class MigIDeferredCreatedRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("MigIColumnRename")]
public class MigIColumnRenameRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationDeferredStepProgressOrderTests
{
    [Fact]
    public void DeferredTableRenameReportsProgressInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigILegacyTable\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
            .Version(2, m => m.RenameTable<MigIRenamedTableRow>("MigILegacyTable"))
            .Migrate();

        Assert.Equal(
        [
            (1, "run SQL", 1, 2),
            (2, "rename table \"MigILegacyTable\" to \"MigIRenamedTable\"", 2, 2),
        ], events);
        Assert.True(db.Schema.TableExists<MigIRenamedTableRow>());
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void DeferredCreateReportsProgressInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Run(_ => { }))
            .Version(2, m => m.CreateTable<MigIDeferredCreatedRow>())
            .Migrate();

        Assert.Equal(
        [
            (1, "run callback at version 1", 1, 2),
            (2, "create \"MigIDeferredCreated\"", 2, 2),
        ], events);
        Assert.True(db.Schema.TableExists<MigIDeferredCreatedRow>());
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void DeferredColumnRenameReportsProgressInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigIColumnRename\" (\"Id\" INTEGER PRIMARY KEY, \"OldName\" TEXT)"))
            .Version(2, m => m.RenameColumn<MigIColumnRenameRow>("OldName", "Name"))
            .Migrate();

        Assert.Equal(
        [
            (1, "run SQL", 1, 2),
            (2, "rename column \"OldName\" to \"Name\" on \"MigIColumnRename\"", 2, 2),
        ], events);
        Assert.True(db.Schema.ColumnExists<MigIColumnRenameRow>("Name"));
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void AnExistingTableRenameAfterAnOpaqueStepReportsInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigILegacyTable\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigIOpaqueMarker\" (\"Id\" INTEGER)"))
            .Version(2, m => m.RenameTable<MigIRenamedTableRow>("MigILegacyTable"))
            .Migrate();

        Assert.Equal(
        [
            (1, "run SQL", 1, 2),
            (2, "rename table \"MigILegacyTable\" to \"MigIRenamedTable\"", 2, 2),
        ], events);
        Assert.True(db.Schema.TableExists<MigIRenamedTableRow>());
    }

    [Fact]
    public void AnExistingColumnRenameAfterAnOpaqueStepReportsInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigIColumnRename\" (\"Id\" INTEGER PRIMARY KEY, \"OldName\" TEXT)");
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigIOpaqueMarker2\" (\"Id\" INTEGER)"))
            .Version(2, m => m.RenameColumn<MigIColumnRenameRow>("OldName", "Name"))
            .Migrate();

        Assert.Equal(
        [
            (1, "run SQL", 1, 2),
            (2, "rename column \"OldName\" to \"Name\" on \"MigIColumnRename\"", 2, 2),
        ], events);
        Assert.True(db.Schema.ColumnExists<MigIColumnRenameRow>("Name"));
    }

    [Fact]
    public void AnExistingTableCreateAfterAnOpaqueStepReportsInApplyOrder()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigIDeferredCreated\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER NOT NULL)");
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigIOpaqueMarker3\" (\"Id\" INTEGER)"))
            .Version(2, m => m.CreateTable<MigIDeferredCreatedRow>())
            .Migrate();

        Assert.Equal(
        [
            (1, "run SQL", 1, 2),
            (2, "create \"MigIDeferredCreated\"", 2, 2),
        ], events);
        Assert.True(db.Schema.TableExists<MigIDeferredCreatedRow>());
    }
}
