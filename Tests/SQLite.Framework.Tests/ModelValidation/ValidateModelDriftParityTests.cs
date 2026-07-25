using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VmdFkParent")]
public sealed class VmdFkParent
{
    [Key]
    public int X { get; set; }

    [Key]
    public int Y { get; set; }
}

[Table("VmdFkChild")]
public sealed class VmdFkChild
{
    [Key]
    public int Id { get; set; }

    public int AId { get; set; }

    public int BId { get; set; }
}

[Table("VmdPk")]
public sealed class VmdPkRow
{
    public int A { get; set; }

    public int B { get; set; }

    public int V { get; set; }
}

[Table("VmdTextPk")]
public sealed class VmdTextPkRow
{
    [Key]
    public required string Code { get; set; }

    public int V { get; set; }
}

[Table("VmdMisParent")]
public sealed class VmdMisParent
{
    [Key]
    public int Id { get; set; }
}

[Table("VmdMisOther")]
public sealed class VmdMisOther
{
    [Key]
    public int Id { get; set; }
}

[Table("VmdMisChild")]
public sealed class VmdMisChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

public class ValidateModelDriftParityTests
{
    [Fact]
    public void CompositeForeignKeySplitIntoTwoSingleForeignKeys_ReportsDrift()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<VmdFkParent>().HasKey(p => new { p.X, p.Y });
            model.Entity<VmdFkChild>().ForeignKey<VmdFkParent>(c => new { c.AId, c.BId }, p => new { p.X, p.Y });
        });
        db.Execute("CREATE TABLE \"VmdFkParent\" (\"X\" INTEGER, \"Y\" INTEGER, PRIMARY KEY (\"X\", \"Y\"))");
        db.Execute("CREATE TABLE \"VmdFkChild\" (\"Id\" INTEGER PRIMARY KEY, \"AId\" INTEGER NOT NULL REFERENCES \"VmdFkParent\"(\"X\"), \"BId\" INTEGER NOT NULL REFERENCES \"VmdFkParent\"(\"Y\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmdFkChild>();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompositePrimaryKeyColumnOrderReversed_ReportsDrift()
    {
        using ModelTestDatabase db = new(model => model.Entity<VmdPkRow>().HasKey(e => new { e.A, e.B }));
        db.Execute("CREATE TABLE \"VmdPk\" (\"A\" INTEGER NOT NULL, \"B\" INTEGER NOT NULL, \"V\" INTEGER NOT NULL, PRIMARY KEY (\"B\", \"A\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmdPkRow>();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompositePrimaryKeyMemberNullabilityDiffers_ReportsDrift()
    {
        using ModelTestDatabase db = new(model => model.Entity<VmdPkRow>().HasKey(e => new { e.A, e.B }));
        db.Execute("CREATE TABLE \"VmdPk\" (\"A\" INTEGER NOT NULL, \"B\" INTEGER, \"V\" INTEGER NOT NULL, PRIMARY KEY (\"A\", \"B\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmdPkRow>();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SingleNonIntegerPrimaryKey_MatchingSchema_IsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<VmdTextPkRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmdTextPkRow>();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ForeignKeyPointingToDifferentTable_ReportsDrift()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmdMisChild>().ForeignKey<VmdMisParent>(c => c.ParentId, p => p.Id));
        db.Execute("CREATE TABLE \"VmdMisParent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"VmdMisOther\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"VmdMisChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"VmdMisOther\"(\"Id\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmdMisChild>();

        Assert.False(result.IsValid);
    }
}
