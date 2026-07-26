using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
[Table("H22cDefaultRegion")]
public class H22cDefaultRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }

    [RTreeAuxiliary]
    [DefaultValue(7)]
    public int Level { get; set; }
}

[FullTextSearch]
[Table("H22cDefaultNote")]
public class H22cDefaultNote
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [DefaultValue("none")]
    public string? Body { get; set; }
}

public class VirtualTableDefaultValueDeclarationTests
{
    [Fact]
    public void ADefaultValueOnAnRTreeAuxiliaryColumnIsRejected()
    {
        using TestDatabase db = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => db.Schema.CreateTable<H22cDefaultRegion>());

        Assert.Contains("H22cDefaultRegion", exception.Message);
    }

    [Fact]
    public void ADefaultValueOnAnFtsIndexedColumnIsRejected()
    {
        using TestDatabase db = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => db.Schema.CreateTable<H22cDefaultNote>());

        Assert.Contains("H22cDefaultNote", exception.Message);
    }
}
