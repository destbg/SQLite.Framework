using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VmTrigSource")]
file sealed class VmTrigSource
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
}

[Table("VmTrigAudit")]
file sealed class VmTrigAudit
{
    [Key]
    public int Id { get; set; }
    public int ItemId { get; set; }
}

[Table("VmFkParent")]
file sealed class VmFkParent
{
    [Key]
    public int Id { get; set; }
}

[Table("VmFkChild")]
file sealed class VmFkChild
{
    [Key]
    public int Id { get; set; }
    public int ParentId { get; set; }
}

file sealed class VmTriggerDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<VmTrigAudit>().HasKey(a => a.Id);
        builder.Entity<VmTrigSource>()
            .Trigger("trg_vm", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<VmTrigAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class ValidateModelGapBugTests
{
    [Fact]
    public void MissingDeclaredTriggerIsReported()
    {
        using VmTriggerDb db = new();
        db.Schema.CreateTable<VmTrigAudit>();
        db.Schema.CreateTable<VmTrigSource>();
        db.Schema.DropTrigger("trg_vm");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmTrigSource>();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ForeignKeyActionMismatchIsReported()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<VmFkParent>().HasKey(p => p.Id);
            model.Entity<VmFkChild>().ForeignKey<VmFkParent>(c => c.ParentId, onDelete: SQLiteForeignKeyAction.Cascade);
        });
        db.Execute("CREATE TABLE \"VmFkParent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"VmFkChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"VmFkParent\"(\"Id\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmFkChild>();

        Assert.False(result.IsValid);
    }
}
