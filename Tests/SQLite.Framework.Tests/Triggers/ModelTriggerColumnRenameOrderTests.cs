using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25nTriggerRenameSources")]
public class H25nTriggerRenameSource
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H25nTriggerRenameAudits")]
public class H25nTriggerRenameAudit
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public sealed class H25nTriggerRenameBodyDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H25nTriggerRenameSource>()
            .Trigger("H25nTrgRenameBody", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
                .Insert(Table<H25nTriggerRenameAudit>(), s => s.Set(a => a.Code, _ => t.New.Code)))
            .HasColumnName(r => r.Code, "cd");
    }
}

public sealed class H25nTriggerRenameGuardDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H25nTriggerRenameSource>()
            .Trigger("H25nTrgRenameGuard", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
                .When(() => t.Old.Code != t.New.Code)
                .Insert(Table<H25nTriggerRenameAudit>(), s => s.Set(a => a.Code, _ => t.New.Code)))
            .HasColumnName(r => r.Code, "cd");
    }
}

public class ModelTriggerColumnRenameOrderTests
{
    [Fact]
    public void ATriggerBodyFollowsALaterColumnRename()
    {
        using H25nTriggerRenameBodyDatabase db = new();
        db.Schema.CreateTable<H25nTriggerRenameAudit>();
        db.Schema.CreateTable<H25nTriggerRenameSource>();

        db.Table<H25nTriggerRenameSource>().Add(new H25nTriggerRenameSource { Id = 1, Code = "abc" });

        List<string> audited = db.Table<H25nTriggerRenameAudit>()
            .OrderBy(a => a.Id)
            .Select(a => a.Code)
            .ToList();

        Assert.Equal(new List<string> { "abc" }, audited);
    }

    [Fact]
    public void ATriggerWhenGuardFollowsALaterColumnRename()
    {
        using H25nTriggerRenameGuardDatabase db = new();
        db.Schema.CreateTable<H25nTriggerRenameAudit>();
        db.Schema.CreateTable<H25nTriggerRenameSource>();

        db.Table<H25nTriggerRenameSource>().Add(new H25nTriggerRenameSource { Id = 1, Code = "abc" });
        db.Table<H25nTriggerRenameSource>().Update(new H25nTriggerRenameSource { Id = 1, Code = "xyz" });

        List<string> audited = db.Table<H25nTriggerRenameAudit>()
            .OrderBy(a => a.Id)
            .Select(a => a.Code)
            .ToList();

        Assert.Equal(new List<string> { "xyz" }, audited);
    }
}
