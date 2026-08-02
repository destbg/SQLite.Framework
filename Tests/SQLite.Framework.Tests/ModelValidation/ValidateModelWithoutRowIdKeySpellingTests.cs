using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[WithoutRowId]
[Table("SecHValNoRowidIntPk")]
public class SecHNoRowidIntPkRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ValidateModelWithoutRowIdKeySpellingTests
{
    [Fact]
    public void WithoutRowIdLiveTableWithIntSpelledKeyValidatesClean()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecHValNoRowidIntPk\" (\"Id\" INT PRIMARY KEY, \"Name\" TEXT NOT NULL) WITHOUT ROWID");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SecHNoRowidIntPkRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void WithoutRowIdModelCreatedTableValidatesClean()
    {
        using TestDatabase db = new();
        db.Table<SecHNoRowidIntPkRow>().Schema.CreateTable();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SecHNoRowidIntPkRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
