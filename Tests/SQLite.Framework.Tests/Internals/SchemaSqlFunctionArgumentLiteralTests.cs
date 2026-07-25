using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21jNormDoc")]
public class H21jNormDoc
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }
}

public class SchemaSqlFunctionArgumentLiteralTests
{
    [Theory]
    [InlineData("replace(\"Code\", 'A', 'B')", "replace(\"Code\", 'a', 'b')")]
    [InlineData("instr(\"Code\", 'A')", "instr(\"Code\", 'a')")]
    [InlineData("hex('A')", "hex('a')")]
    [InlineData("( 'A' )", "( 'a' )")]
    [InlineData("\"Code\" || 'A'", "\"Code\" || 'a'")]
    [InlineData("\"Code\" like 'a%' escape 'A'", "\"Code\" like 'a%' escape 'a'")]
    [InlineData("\"Code\" in ('a', hex(\"Code\"), 'B')", "\"Code\" in ('a', hex(\"Code\"), 'b')")]
    [InlineData("values ('a', hex(\"Code\"), 'B')", "values ('a', hex(\"Code\"), 'b')")]
    public void LiteralCaseInsideACallIsNotEquivalent(string expected, string actual)
    {
        Assert.False(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Theory]
    [InlineData("instr(\"Code\", 'AB')", "instr(\"Code\", \"AB\")")]
    [InlineData("replace(\"Code\", 'AB', 'C')", "replace(\"Code\", [AB], 'C')")]
    public void LiteralIsNotEquivalentToAColumnReference(string expected, string actual)
    {
        Assert.False(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Fact]
    public void ExpressionIndexWithADifferentLiteralCaseIsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<H21jNormDoc>().Index(r => r.Code.Replace("A", "B"), name: "IX_H21jNormDocCode"));
        db.Schema.CreateTable<H21jNormDoc>();

        Assert.NotEqual(
            db.ExecuteScalar<string>("SELECT REPLACE('xAx', 'A', 'B')"),
            db.ExecuteScalar<string>("SELECT REPLACE('xAx', 'a', 'b')"));

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_H21jNormDocCode'")!;
        string drifted = declared.Replace("'A'", "'a'").Replace("'B'", "'b'");
        Assert.NotEqual(declared, drifted);

        db.Schema.DropIndex("IX_H21jNormDocCode");
        db.Execute(drifted);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21jNormDoc>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void PartialIndexFilterWithADifferentLiteralCaseIsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<H21jNormDoc>().Index(r => r.Id, name: "IX_H21jNormDocId", filter: r => r.Code.Replace("A", "B") == r.Code));
        db.Schema.CreateTable<H21jNormDoc>();

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_H21jNormDocId'")!;
        string drifted = declared.Replace("'A'", "'a'").Replace("'B'", "'b'");
        Assert.NotEqual(declared, drifted);

        db.Schema.DropIndex("IX_H21jNormDocId");
        db.Execute(drifted);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21jNormDoc>();

        Assert.False(result.IsValid, string.Join(" | ", result.Issues));
    }
}
