using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecK_ValidatedGenerated")]
public class SecKValidatedGeneratedRow
{
    [Key]
    public int Id { get; set; }

    public double Price { get; set; }

    public int Quantity { get; set; }
}

public class ValidateModelExtraGeneratedColumnTests
{
    [Fact]
    public void ExtraLiveGeneratedColumnIsReported()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecK_ValidatedGenerated\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL GENERATED ALWAYS AS (\"Price\" * \"Quantity\") VIRTUAL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SecKValidatedGeneratedRow>();

        Assert.Contains(result.Issues, i => i.Contains("Total"));
    }

    [Fact]
    public void ExtraLiveRegularColumnIsReportedControl()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecK_ValidatedGenerated\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SecKValidatedGeneratedRow>();

        Assert.Contains(result.Issues, i => i.Contains("Total"));
    }
}
