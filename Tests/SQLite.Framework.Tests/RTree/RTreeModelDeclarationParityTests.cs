using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RTreeModelDeclarationParityTests
{
    [Fact]
    public void ModelIndexOnRTreeEntityThrows()
    {
        using H20RtIndexModelDb db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H20RtGuardRegion>());
    }

    [Fact]
    public void ModelTriggerOnRTreeEntityThrows()
    {
        using H20RtTriggerModelDb db = new();
        db.Table<H20RtAuditRow>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H20RtGuardRegion>());
    }

    [Fact]
    public void ModelCheckOnRTreeEntityThrows()
    {
        using H20RtCheckModelDb db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H20RtGuardRegion>());
    }

    [Fact]
    public void ModelComputedColumnOnRTreeEntityThrows()
    {
        using H20RtComputedModelDb db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H20RtGuardRegion>());
    }
}

[RTreeIndex]
[Table("H20RtGuardRegion")]
public class H20RtGuardRegion
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
    [RTreeAuxiliary] public string? Label { get; set; }
}

[Table("H20RtAuditRow")]
public class H20RtAuditRow
{
    [Key, AutoIncrement] public int Id { get; set; }
    public int RegionId { get; set; }
}

file sealed class H20RtIndexModelDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H20RtGuardRegion>().Index(r => r.Label, "ix_h20rtguard_label");
    }
}

file sealed class H20RtTriggerModelDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H20RtGuardRegion>().Trigger("trg_h20rtguard", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update,
            t => t.Insert(Table<H20RtAuditRow>(), s => s.Set(a => a.RegionId, _ => t.New.Id)));
    }
}

file sealed class H20RtCheckModelDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H20RtGuardRegion>().Check(r => r.MaxX >= r.MinX, "ck_h20rtguard_bounds");
    }
}

file sealed class H20RtComputedModelDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H20RtGuardRegion>().Computed(r => r.Label, r => r.Label);
    }
}
