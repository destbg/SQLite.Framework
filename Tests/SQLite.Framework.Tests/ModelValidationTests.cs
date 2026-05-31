using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ModelValidationTests
{
    [Fact]
    public void ValidateModel_MatchingSchema_IsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateModel_MissingTable_ReportsIssue()
    {
        using TestDatabase db = new();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("does not exist"));
    }

    [Fact]
    public void ValidateModel_FtsTable_ChecksExistenceOnly()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SimpleSearchEntity>();

        Assert.True(db.Schema.ValidateModel<SimpleSearchEntity>().IsValid);
    }

    [Fact]
    public void ValidateModel_RTreeTable_ChecksExistenceOnly()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2D>();

        Assert.True(db.Schema.ValidateModel<Region2D>().IsValid);
    }

    [Fact]
    public void ValidateModel_MissingColumn_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"Books\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("BookPrice") && i.Contains("missing"));
    }

    [Fact]
    public void ValidateModel_TypeMismatch_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"Books\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" TEXT NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("BookPrice") && i.Contains("has type"));
    }

    [Fact]
    public void ValidateModel_NullabilityMismatch_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"Books\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("BookTitle") && i.Contains("nullability"));
    }

    [Fact]
    public void ValidateModel_PrimaryKeyMismatch_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"Books\" (\"BookId\" INTEGER, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("BookId") && i.Contains("primary-key"));
    }

    [Fact]
    public void ValidateModel_ExtraColumn_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"Books\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, \"Junk\" TEXT)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("Junk") && i.Contains("not in the model"));
    }

    [Fact]
    public void ValidateModel_MissingIndex_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.DropIndex("IX_Book_AuthorId");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.Contains(result.Issues, i => i.Contains("IX_Book_AuthorId") && i.Contains("missing"));
    }

    [Fact]
    public void ValidateModel_ForeignKeyPresent_IsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBook>();

        Assert.True(db.Schema.ValidateModel<FkBook>().IsValid);
    }

    [Fact]
    public void ValidateModel_MissingForeignKey_ReportsIssue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"FkBook\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT NOT NULL, \"AuthorId\" INTEGER NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<FkBook>();

        Assert.Contains(result.Issues, i => i.Contains("Foreign key") && i.Contains("AuthorId"));
    }

    [Fact]
    public void ValidateModel_CompositeForeignKeyPresent_IsValid()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { l.OrderId, l.OrderVersion },
                o => new { o.Id, o.Version },
                onDelete: SQLiteForeignKeyAction.Cascade));
        db.Schema.CreateTable<FkOrder>();
        db.Schema.CreateTable<FkOrderLine>();

        Assert.True(db.Schema.ValidateModel<FkOrderLine>().IsValid);
    }

    [Fact]
    public async Task ValidateModelAsync_MatchingSchema_IsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        SQLiteModelValidationResult result = await db.Schema.ValidateModelAsync<Book>(TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }
}
