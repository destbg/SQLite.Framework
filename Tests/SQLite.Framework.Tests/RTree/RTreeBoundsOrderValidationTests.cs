using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
[Table("H21jOrderRegion")]
public class H21jOrderRegion
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
}

public class RTreeBoundsOrderValidationTests
{
    [Fact]
    public void BoundsDeclaredInTheWrongSlotOrderReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIRTUAL TABLE \"H21jOrderRegion\" USING rtree(\"Id\", \"MaxX\", \"MinX\", \"MinY\", \"MaxY\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21jOrderRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void BoundsDeclaredInTheWrongSlotOrderRejectsAModelRow()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIRTUAL TABLE \"H21jOrderRegion\" USING rtree(\"Id\", \"MaxX\", \"MinX\", \"MinY\", \"MaxY\")");

        Assert.Throws<SQLiteException>(() => db.Table<H21jOrderRegion>().Add(
            new H21jOrderRegion { Id = 1, MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 }));
    }
}
