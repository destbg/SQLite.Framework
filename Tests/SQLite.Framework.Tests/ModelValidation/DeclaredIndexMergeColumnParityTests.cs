using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24eIndexMergeRows")]
public class H24eIndexMergeRow
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    public string Code { get; set; } = "";

    public string Kind { get; set; } = "";
}

[Table("H24eDoubleIndexedRows")]
public class H24eDoubleIndexedRow
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    [Indexed(IsUnique = true)]
    public string Code { get; set; } = "";
}

public class DeclaredIndexMergeColumnParityTests
{
    [Fact]
    public void ValidateModelIsCleanWhenAFluentIndexAddsACollationToAnAttributeIndex()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eIndexMergeRow>()
            .Index(r => r.Code, collation: SQLiteCollation.NoCase));
        db.Schema.CreateTable<H24eIndexMergeRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24eIndexMergeRow>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void ValidateModelIsCleanWhenTwoNamedIndexesRepeatOneColumnWithDifferentDirections()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eIndexMergeRow>()
            .Index(r => r.Kind, name: "IXH24eMergeDir")
            .Index(r => r.Kind, name: "IXH24eMergeDir", direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<H24eIndexMergeRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24eIndexMergeRow>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void ValidateModelIsCleanWhenOnePropertyCarriesTwoIndexedAttributes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H24eDoubleIndexedRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24eDoubleIndexedRow>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void AnIndexDeclaredTwiceOverOneColumnIndexesThatColumnOnce()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eIndexMergeRow>()
            .Index(r => r.Kind, name: "IXH24eMergeOnce")
            .Index(r => r.Kind, name: "IXH24eMergeOnce", direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<H24eIndexMergeRow>();

        List<string> indexedColumns = db.Query<Dictionary<string, object?>>("PRAGMA index_info('IXH24eMergeOnce')")
            .Select(row => (string)row["name"]!)
            .ToList();

        Assert.Equal(new List<string> { "Kind" }, indexedColumns);
    }
}
