using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class MvExpressionIndexRow
{
    [Key]
    public long Id { get; set; }

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";
}

public class ModelValidationExpressionIndexParityTests
{
    [Fact]
    public void MixedExpressionIndexMatchingLiveSchema_IsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<MvExpressionIndexRow>().Index(r => new { r.LastName, Upper = r.FirstName.ToUpper() }, name: "idx_mv_expr_name"));
        db.Schema.CreateTable<MvExpressionIndexRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<MvExpressionIndexRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void PureExpressionIndexMatchingLiveSchema_IsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<MvExpressionIndexRow>().Index(r => r.FirstName.ToUpper(), name: "idx_mv_expr_upper"));
        db.Schema.CreateTable<MvExpressionIndexRow>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<MvExpressionIndexRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
