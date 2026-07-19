using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RTreeModelValidationTests
{
    [Fact]
    public void PlainTableWithRTreeModelReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H20RtValRegion\" (\"Id\" INTEGER PRIMARY KEY, \"MinX\" REAL, \"MaxX\" REAL, \"MinY\" REAL, \"MaxY\" REAL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void RTreeTableMissingAuxColumnReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIRTUAL TABLE \"H20RtValRegion\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void RTreeTableWithExtraColumnReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIRTUAL TABLE \"H20RtValRegion\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\", +\"Label\", +\"Extra\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void MatchingRTreeTableReportsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H20RtValRegion>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void ViewUnderRTreeModelReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIEW \"H20RtValRegion\" AS SELECT 1 AS \"Id\", 2.0 AS \"MinX\", 3.0 AS \"MaxX\", 4.0 AS \"MinY\", 5.0 AS \"MaxY\", 'x' AS \"Label\"");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
        Assert.Equal(["Table 'H20RtValRegion' is not an R-Tree table."], result.Issues);
    }

    [Fact]
    public void Int32StorageTableAgainstFloatModelReportsTypeDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIRTUAL TABLE \"H20RtValRegion\" USING rtree_i32(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\", +\"Label\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20RtValRegion>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
        Assert.Equal(
            [
                "Column 'H20RtValRegion'.'MinX' has type 'INT' but the model expects 'REAL'.",
                "Column 'H20RtValRegion'.'MaxX' has type 'INT' but the model expects 'REAL'.",
                "Column 'H20RtValRegion'.'MinY' has type 'INT' but the model expects 'REAL'.",
                "Column 'H20RtValRegion'.'MaxY' has type 'INT' but the model expects 'REAL'."
            ],
            result.Issues);
    }
}

[RTreeIndex]
[Table("H20RtValRegion")]
public class H20RtValRegion
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
    [RTreeAuxiliary] public string? Label { get; set; }
}
