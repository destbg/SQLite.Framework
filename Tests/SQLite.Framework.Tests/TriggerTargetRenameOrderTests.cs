using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TrRenameAudit")]
public class TrRenameAuditRow
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }
}

[Table("TrRenameSource")]
public class TrRenameSourceRow
{
    [Key]
    public int Id { get; set; }
}

file sealed class TrRenameDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<TrRenameSourceRow>()
            .Trigger("trg_rename_order", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<TrRenameAuditRow>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
        builder.Entity<TrRenameAuditRow>().ToTable("TrRenameAuditFinal");
    }
}

public class TriggerTargetRenameOrderTests
{
    [Fact]
    public void TriggerKeepsTargetTableNameFromDeclarationTime()
    {
        using TrRenameDb db = new();
        db.Schema.CreateTable<TrRenameAuditRow>();
        db.Schema.CreateTable<TrRenameSourceRow>();

        Assert.Throws<SQLiteException>(() => db.Table<TrRenameSourceRow>().Add(new TrRenameSourceRow { Id = 1 }));
    }
}
