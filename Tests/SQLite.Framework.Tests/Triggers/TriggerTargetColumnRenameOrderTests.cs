using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26jTriggerSources")]
public class H26jTriggerSource
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H26jTriggerAudits")]
public class H26jTriggerAudit
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H26jSelfTriggerRows")]
public class H26jSelfTriggerRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Total { get; set; }
}

public sealed class H26jCrossTableTriggerDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H26jTriggerSource>()
            .Trigger("H26jTrgCrossColumn", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
                .Insert(Table<H26jTriggerAudit>(), s => s.Set(a => a.Code, _ => t.New.Code)));

        builder.Entity<H26jTriggerAudit>()
            .HasColumnName(a => a.Code, "cd");
    }
}

public sealed class H26jSelfTargetTriggerDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H26jSelfTriggerRow>()
            .Trigger("H26jTrgSelfColumn", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
                .Update(Table<H26jSelfTriggerRow>(), r => r.Id == t.New.Id, s => s.Set(r => r.Total, _ => t.New.Price * 2)))
            .HasColumnName(r => r.Total, "tot");
    }
}

public class TriggerTargetColumnRenameOrderTests
{
    [Fact]
    public void ATriggerBodyFollowsALaterColumnRenameOnTheTableItWritesTo()
    {
        using H26jCrossTableTriggerDatabase db = new();
        db.Schema.CreateTable<H26jTriggerAudit>();
        db.Schema.CreateTable<H26jTriggerSource>();

        db.Table<H26jTriggerSource>().AddRange(SourceRows());

        List<string> expected = SourceRows()
            .Select(r => r.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H26jTriggerAudit>()
            .Select(a => a.Code)
            .AsEnumerable()
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ATriggerBodyFollowsALaterColumnRenameOnItsOwnTable()
    {
        using H26jSelfTargetTriggerDatabase db = new();
        db.Schema.CreateTable<H26jSelfTriggerRow>();

        db.Table<H26jSelfTriggerRow>().AddRange(SelfRows());

        List<int> expected = SelfRows()
            .Select(r => r.Price * 2)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26jSelfTriggerRow>()
            .Select(r => r.Total)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26jTriggerSource> SourceRows()
    {
        return
        [
            new H26jTriggerSource { Id = 1, Code = "alpha" },
            new H26jTriggerSource { Id = 2, Code = "beta" }
        ];
    }

    private static List<H26jSelfTriggerRow> SelfRows()
    {
        return
        [
            new H26jSelfTriggerRow { Id = 1, Price = 5 },
            new H26jSelfTriggerRow { Id = 2, Price = 11 }
        ];
    }
}
