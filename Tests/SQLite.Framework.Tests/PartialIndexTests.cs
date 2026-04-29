using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PartialIndexTests
{
    [Fact]
    public void Index_WithoutFilter_CreatesPlainIndex()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Index(b => b.Title, name: "IX_Title")
            .CreateTable();

        Assert.True(db.Schema.IndexExists("IX_Title"));
    }

    [Fact]
    public void Index_Unique_RejectsDuplicates()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Index(b => b.Title, name: "IX_Title_Unique", unique: true)
            .CreateTable();

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "same", AuthorId = 1, Price = 1 });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "same", AuthorId = 1, Price = 2 }));
    }

    [Fact]
    public void Index_WithFilter_CreatesPartialIndex()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Index(b => b.Title, name: "IX_Title_Active", filter: b => b.Price > 0)
            .CreateTable();

        Assert.True(db.Schema.IndexExists("IX_Title_Active"));

        // Verify it is a partial index by checking the SQL stored in sqlite_master
        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Title_Active'");

        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Index_DefaultName_FollowsConvention()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Index(b => b.Title)
            .CreateTable();

        Assert.Contains("idx_BooksArchive_BookTitle", db.Schema.ListIndexes("BooksArchive"));
    }

    [Fact]
    public void Index_PartialUnique_AllowsDuplicateOutsideFilter()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Index(b => b.Title, name: "IX_ActiveTitleUnique", unique: true, filter: b => b.AuthorId > 0)
            .CreateTable();

        // Both rows have AuthorId = 0 so neither falls under the partial unique index
        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "same", AuthorId = 0, Price = 1 });
        db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "same", AuthorId = 0, Price = 2 });

        Assert.Equal(2, db.Table<BookArchive>().Count());

        // Now add one inside the filter
        db.Table<BookArchive>().Add(new BookArchive { Id = 3, Title = "different", AuthorId = 1, Price = 1 });

        // Adding another inside the filter with the same title should fail
        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 4, Title = "different", AuthorId = 2, Price = 1 }));
    }
}
