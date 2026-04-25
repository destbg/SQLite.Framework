using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteTableBuilderTests
{
    [Fact]
    public void Builder_AllFeaturesChained_Creates()
    {
        using TestDatabase db = new();

        db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true)
            .Check(p => p.Quantity >= 0, name: "CK_Qty")
            .Index(p => p.Price, name: "IX_Price")
            .Create();

        Assert.True(db.Schema.TableExists<ProductLine>());
        Assert.True(db.Schema.IndexExists("IX_Price"));

        db.Execute("INSERT INTO ProductLines (Id, Price, Quantity) VALUES (1, 4.0, 2)");

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(8.0m, row.Total);

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO ProductLines (Id, Price, Quantity) VALUES (2, 1.0, -5)"));
    }

    [Fact]
    public void Builder_NullColumnExpression_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() =>
            db.Schema.Table<BookArchive>().Index<string>(null!));
    }

    [Fact]
    public void Builder_NullCheckPredicate_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() =>
            db.Schema.Table<BookArchive>().Check(null!));
    }

    [Fact]
    public void Builder_NullComputedExpression_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() =>
            db.Schema.Table<ProductLine>().Computed(p => p.Total, null!));
    }

    [Fact]
    public async Task Builder_CreateAsync_RoundTrips()
    {
        using TestDatabase db = new();

        await db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity)
            .Index(p => p.Price, name: "IX_Price_Async")
            .CreateAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<ProductLine>());
        Assert.True(db.Schema.IndexExists("IX_Price_Async"));
    }

    [Fact]
    public void Builder_FtsTable_ThrowsInvalidOperation()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.Table<ArticleSearch>().Create());
    }

    [Fact]
    public void Builder_WithoutRowId_AppendsClause()
    {
        using TestDatabase db = new();

        db.Schema.Table<WithoutRowIdEntity>().Create();

        Assert.True(db.Schema.TableExists<WithoutRowIdEntity>());

        // Round-trip a row to confirm the table works
        db.Execute(
            "INSERT INTO WithoutRowIdEntity (Code, Name) VALUES ('a', 'first')");
        WithoutRowIdEntity row = db.Table<WithoutRowIdEntity>().Single();
        Assert.Equal("a", row.Code);
    }

    [Fact]
    public void Builder_EmitsIndexesFromIndexedAttribute()
    {
        using TestDatabase db = new();

        db.Schema.Table<Book>().Create();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("Books");
        Assert.Contains("IX_Book_AuthorId", indexes);

        // Adding a duplicate price should fail because Book.Price has [Indexed(IsUnique = true)]
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 9.99 });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 9.99 }));
    }

    [Fact]
    public void Builder_FluentIndex_Unique_RejectsDuplicates()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Index(b => b.AuthorId, name: "IX_BookArchive_Author_Unique", unique: true)
            .Create();

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "a", AuthorId = 7, Price = 1 });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "b", AuthorId = 7, Price = 2 }));
    }

    [Fact]
    public void Builder_Index_UnwrapsConvertOnColumnExpression()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Index<object?>(b => b.Price, name: "IX_BoxedPrice")
            .Create();

        Assert.True(db.Schema.IndexExists("IX_BoxedPrice"));
    }

    [Fact]
    public void Builder_NamedCheckConstraint_EmitsConstraintName()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Builder_Price")
            .Create();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Contains("CONSTRAINT", sql);
        Assert.Contains("CK_Builder_Price", sql);
    }
}
