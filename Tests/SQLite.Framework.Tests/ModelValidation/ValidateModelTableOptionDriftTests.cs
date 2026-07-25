using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[WithoutRowId]
[Table("H21zValNoRowid")]
public class H21zNoRowidRow
{
    [Key]
    [MaxLength(20)]
    public required string Code { get; set; }

    public int Value { get; set; }
}

[Table("H21zValPlain")]
public class H21zPlainRow
{
    [Key]
    [MaxLength(20)]
    public required string Code { get; set; }

    public int Value { get; set; }
}

[StrictTable]
[Table("H21zValStrict")]
public class H21zStrictRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class ValidateModelTableOptionDriftTests
{
    [Fact]
    public void ModelWithoutRowIdAgainstPlainLiveTableReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21zValNoRowid\" (\"Code\" TEXT NOT NULL, \"Value\" INTEGER NOT NULL, PRIMARY KEY (\"Code\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21zNoRowidRow>();

        Assert.False(result.IsValid, "validator reported clean: " + string.Join(" | ", result.Issues));
    }

    [Fact]
    public void PlainModelAgainstWithoutRowIdLiveTableReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21zValPlain\" (\"Code\" TEXT NOT NULL, \"Value\" INTEGER NOT NULL, PRIMARY KEY (\"Code\")) WITHOUT ROWID");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21zPlainRow>();

        Assert.False(result.IsValid, "validator reported clean: " + string.Join(" | ", result.Issues));
    }

    [Fact]
    public void StrictModelAgainstNonStrictLiveTableReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21zValStrict\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21zStrictRow>();

        Assert.False(result.IsValid, "validator reported clean: " + string.Join(" | ", result.Issues));
    }

    [Fact]
    public void ModelCreatedTablesValidateClean()
    {
        using TestDatabase db = new();
        db.Table<H21zNoRowidRow>().Schema.CreateTable();
        db.Table<H21zPlainRow>().Schema.CreateTable();
        db.Table<H21zStrictRow>().Schema.CreateTable();

        SQLiteModelValidationResult a = db.Schema.ValidateModel<H21zNoRowidRow>();
        SQLiteModelValidationResult b = db.Schema.ValidateModel<H21zPlainRow>();
        SQLiteModelValidationResult c = db.Schema.ValidateModel<H21zStrictRow>();

        Assert.True(a.IsValid, string.Join(" | ", a.Issues));
        Assert.True(b.IsValid, string.Join(" | ", b.Issues));
        Assert.True(c.IsValid, string.Join(" | ", c.Issues));
    }
}
