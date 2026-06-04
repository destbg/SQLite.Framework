using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ValidateModelTests
{
    [Fact]
    public void ShadowColumnIsNotFlaggedAsExtra()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<Book>().Column("RowVersion", SQLiteColumnType.Integer, nullable: false, defaultSql: "0"));
        db.Schema.CreateTable<Book>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void ComputedColumnPassesValidation()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<ProductLine>().Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<ProductLine>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void MissingBuilderDeclaredIndexIsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<Book>().Index(b => b.Title, name: "IX_Book_Title"));
        db.Schema.CreateTable<Book>();
        db.Schema.DropIndex("IX_Book_Title");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.False(result.IsValid);
    }
}
