using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ValStrictLoose")]
public class ValStrictLooseRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

[RTreeIndex(SQLiteRTreeStorage.Int32)]
[Table("ValRTreeInt")]
public class ValRTreeIntRow
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("x")]
    public int MinX { get; set; }

    [RTreeMax("x")]
    public int MaxX { get; set; }
}

public class ValidateModelStrictAndRTreeStorageDriftTests
{
    [Fact]
    public void StrictLiveTableAgainstALooseModelReportsDrift()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"ValStrictLoose\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL) STRICT");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<ValStrictLooseRow>();

        Assert.False(result.IsValid, "validator reported clean: " + string.Join(" | ", result.Issues));
        Assert.Contains(result.Issues, i => i.Contains("STRICT in the database", StringComparison.Ordinal));
    }

    [Fact]
    public void IntegerRTreeMatchesItsOwnCreatedTable()
    {
        using TestDatabase db = new();
        db.Table<ValRTreeIntRow>().Schema.CreateTable();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<ValRTreeIntRow>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }
}
