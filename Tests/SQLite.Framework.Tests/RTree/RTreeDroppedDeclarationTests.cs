using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21jRefOwner")]
public class H21jRefOwner
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[RTreeIndex]
[Table("H21jRefRegion")]
public class H21jRefRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }

    [RTreeAuxiliary]
    [ReferencesTable(typeof(H21jRefOwner), OnDelete = SQLiteForeignKeyAction.Cascade)]
    public int OwnerId { get; set; }
}

[RTreeIndex]
[Table("H21jShadowRegion")]
public class H21jShadowRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

public class RTreeDroppedDeclarationTests
{
    [Fact]
    public void ForeignKeyOnAnRTreeEntityReportsAModelError()
    {
        using TestDatabase db = new();
        db.Table<H21jRefOwner>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H21jRefRegion>());
    }

    [Fact]
    public void ExtraColumnOnAnRTreeEntityReportsAModelError()
    {
        using H21jRtShadowDb db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H21jShadowRegion>());
    }
}

file sealed class H21jRtShadowDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H21jShadowRegion>().Column("Extra", SQLiteColumnType.Text);
    }
}
