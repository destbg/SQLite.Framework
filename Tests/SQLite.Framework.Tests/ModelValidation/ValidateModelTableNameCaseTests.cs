using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[WithoutRowId]
[Table("H22bCaseVaults")]
public class H22bCaseVault
{
    [Key]
    public required string Code { get; set; }

    public int Value { get; set; }
}

[StrictTable]
[Table("H22bCaseLedgers")]
public class H22bCaseLedger
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class ValidateModelTableNameCaseTests
{
    [Fact]
    public void WithoutRowIdModelIsValidWhenTheLiveTableNameDiffersInCase()
    {
        using TestDatabase db = new();
        db.Execute("create table h22bcasevaults (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Value\" INTEGER NOT NULL) without rowid");

        Assert.Equal(
            "CREATE TABLE h22bcasevaults (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Value\" INTEGER NOT NULL) without rowid",
            db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H22bCaseVaults' COLLATE NOCASE"));

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H22bCaseVault>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void StrictModelIsValidWhenTheLiveTableNameDiffersInCase()
    {
        using TestDatabase db = new();
        db.Execute("create table h22bcaseledgers (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL) strict");

        Assert.Equal(
            "CREATE TABLE h22bcaseledgers (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL) strict",
            db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H22bCaseLedgers' COLLATE NOCASE"));

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H22bCaseLedger>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }
}
