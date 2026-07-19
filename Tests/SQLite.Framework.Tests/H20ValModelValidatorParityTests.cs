using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20ValIntPkRow")]
public class H20ValIntPkRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("H20ValIdxRow")]
public class H20ValIdxRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }
}

public class H20ValModelValidatorParityTests
{
    [Fact]
    public void LiveIntPrimaryKeyIsNotARowIdAliasAndIsReported()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H20ValIntPkRow\" (\"Id\" INT PRIMARY KEY, \"Name\" TEXT NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20ValIntPkRow>();

        Assert.False(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void SingleQuotedIdentifiersInLiveIndexAreEquivalent()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<H20ValIdxRow>().Index(r => r.Code, name: "IX_H20ValIdxCode"));
        db.Schema.CreateTable<H20ValIdxRow>();
        db.Schema.DropIndex("IX_H20ValIdxCode");
        db.Execute("CREATE INDEX 'IX_H20ValIdxCode' ON 'H20ValIdxRow' ('Code')");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H20ValIdxRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
