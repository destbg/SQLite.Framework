using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VmDriftAudit")]
public class VmDriftAuditRow
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }
}

[Table("VmDriftSource")]
public class VmDriftSourceRow
{
    [Key]
    public int Id { get; set; }
}

file sealed class VmDriftDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<VmDriftAuditRow>().HasKey(a => a.Id);
        builder.Entity<VmDriftSourceRow>()
            .Trigger("trg_drift", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<VmDriftAuditRow>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class ModelValidationTriggerBodyDriftTests
{
    [Fact]
    public void TriggerBodyDriftIsReported()
    {
        using VmDriftDb db = new();
        db.Schema.CreateTable<VmDriftAuditRow>();
        db.Schema.CreateTable<VmDriftSourceRow>();
        db.Schema.DropTrigger("trg_drift");
        db.Execute("CREATE TRIGGER \"trg_drift\" AFTER INSERT ON \"VmDriftSource\" BEGIN SELECT 1; END");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmDriftSourceRow>();

        Assert.False(result.IsValid);
    }
}
