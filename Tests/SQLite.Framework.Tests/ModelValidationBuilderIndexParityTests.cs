using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class MvBuilderIndexRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Score { get; set; }
}

public class ModelValidationBuilderIndexParityTests
{
    [Fact]
    public void BuilderIndexMatchingLiveSchema_IsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<MvBuilderIndexRow>().Index(r => r.Name, name: "IX_MvBuilderIndex_Name"));
        db.Schema.CreateTable<MvBuilderIndexRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<MvBuilderIndexRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void BuilderMultiColumnIndexMatchingLiveSchema_IsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<MvBuilderIndexRow>().Index(r => new { r.Name, r.Score }, name: "IX_MvBuilderIndex_NameScore"));
        db.Schema.CreateTable<MvBuilderIndexRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<MvBuilderIndexRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
