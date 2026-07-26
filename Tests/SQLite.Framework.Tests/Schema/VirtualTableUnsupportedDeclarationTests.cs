using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VtabParentKeys")]
public class VtabParentKey
{
    [Key]
    [Column(Order = 0)]
    public int RegionId { get; set; }

    [Key]
    [Column(Order = 1)]
    public int ZoneId { get; set; }
}

[RTreeIndex]
[Table("VtabCompositeRegions")]
public class VtabCompositeRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }

    [RTreeAuxiliary]
    public int RegionId { get; set; }

    [RTreeAuxiliary]
    public int ZoneId { get; set; }
}

[WithoutRowId]
[RTreeIndex]
[Table("VtabNoRowidRegions")]
public class VtabNoRowidRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

[StrictTable]
[RTreeIndex]
[Table("VtabStrictRegions")]
public class VtabStrictRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

public class VirtualTableUnsupportedDeclarationTests
{
    [Fact]
    public void CompositeForeignKeyOnAnRTreeEntityThrows()
    {
        using VtabCompositeDb db = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => db.Schema.CreateTable<VtabCompositeRegion>());

        Assert.Contains("a foreign key", exception.Message);
    }

    [Fact]
    public void WithoutRowIdOnAnRTreeEntityThrows()
    {
        using TestDatabase db = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => db.Schema.CreateTable<VtabNoRowidRegion>());

        Assert.Contains("WITHOUT ROWID", exception.Message);
    }

    [Fact]
    public void StrictOnAnRTreeEntityThrows()
    {
        using TestDatabase db = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => db.Schema.CreateTable<VtabStrictRegion>());

        Assert.Contains("STRICT", exception.Message);
    }
}

file sealed class VtabCompositeDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<VtabCompositeRegion>()
            .ForeignKey<VtabParentKey>(r => new { r.RegionId, r.ZoneId });
    }
}
