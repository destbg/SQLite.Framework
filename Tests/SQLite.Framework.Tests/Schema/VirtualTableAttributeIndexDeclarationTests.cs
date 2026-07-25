using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
[Table("H21aAttrRegion")]
public class H21aAttrRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }

    [RTreeMin("Y")]
    public float MinY { get; set; }

    [RTreeMax("Y")]
    public float MaxY { get; set; }

    [Indexed]
    [RTreeAuxiliary]
    public string? Label { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[Table("H21aAttrSearch")]
public class H21aAttrSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [Indexed]
    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class VirtualTableAttributeIndexDeclarationTests
{
    [Fact]
    public void AttributeIndexOnAnRTreeEntityThrows()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H21aAttrRegion>());
    }

    [Fact]
    public void AttributeIndexOnAFullTextSearchEntityThrows()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H21aAttrSearchRow>());
    }
}
