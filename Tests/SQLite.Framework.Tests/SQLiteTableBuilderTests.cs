using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
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
            .CreateTable();

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
            .CreateTableAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<ProductLine>());
        Assert.True(db.Schema.IndexExists("IX_Price_Async"));
    }

    [Fact]
    public void Builder_FtsTable_DelegatesToSchema()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();

        db.Schema.Table<ArticleSearch>().CreateTable();

        Assert.True(db.Schema.TableExists<ArticleSearch>());
    }

    [Fact]
    public void Builder_WithoutRowId_AppendsClause()
    {
        using TestDatabase db = new();

        db.Schema.Table<WithoutRowIdEntity>().CreateTable();

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

        db.Schema.Table<Book>().CreateTable();

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
            .CreateTable();

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
            .CreateTable();

        Assert.True(db.Schema.IndexExists("IX_BoxedPrice"));
    }

    [Fact]
    public void Builder_Check_StringConstant_InlinesAsLiteral()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Check(b => b.Title != "")
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Contains("''", sql);
        Assert.DoesNotContain("@p", sql);
    }

    [Fact]
    public void Builder_Check_DoubleConstant_InlinesAsLiteral()
    {
        using TestDatabase db = new();

        db.Schema.Table<ProductLine>()
            .Check(p => p.Quantity > 0)
            .Check(p => p.Price > 0.5m)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Contains("0.5", sql);
        Assert.DoesNotContain("@p", sql);
    }

    [Fact]
    public void Builder_Check_StringWithApostrophe_DoublesQuote()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Check(b => b.Title != "O'Reilly")
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Contains("'O''Reilly'", sql);
    }

    [Fact]
    public void Builder_Check_UntranslatableExpression_Throws()
    {
        using TestDatabase db = new();

        Guid id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<BookArchive>()
                .Check(b => b.Title != id.ToString().Substring(0, 8) || b.AuthorId == 1)
                .CreateTable());
    }

    [Fact]
    public void Builder_Check_LongConstant_InlinesAsLiteral()
    {
        using TestDatabase db = new();

        long limit = 1234567890123L;
        db.Schema.Table<ProductLine>()
            .Check(p => (long)p.Quantity < limit)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Contains("1234567890123", sql);
    }

    [Fact]
    public void Builder_Check_FloatConstant_InlinesAsLiteral()
    {
        using TestDatabase db = new();

        float threshold = 2.5f;
        db.Schema.Table<ProductLine>()
            .Check(p => (float)p.Price > threshold)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Contains("2.5", sql);
    }

    [Fact]
    public void Builder_Check_BoolConstant_InlinesAsLiteral()
    {
        using TestDatabase db = new();

        db.Schema.Table<ProductLine>()
            .Check(p => (p.Quantity > 0) == true)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Contains("= 1", sql);
    }

    [Fact]
    public void Builder_Check_DateTimeConstant_ThrowsNotSupported()
    {
        using TestDatabase db = new();

        DateTime cutoff = new(2026, 1, 1);
        Assert.Throws<NotSupportedException>(() =>
            db.Schema.Table<Article>()
                .Check(a => a.PublishedAt > cutoff)
                .CreateTable());
    }

    [Fact]
    public void Builder_NamedCheckConstraint_EmitsConstraintName()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Builder_Price")
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Contains("CONSTRAINT", sql);
        Assert.Contains("CK_Builder_Price", sql);
    }

    [Fact]
    public void Builder_FtsTable_WithComputedExtras_Throws()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.Table<ArticleSearch>()
                .Computed(a => a.Title, a => a.Title)
                .CreateTable());
        Assert.Contains("ArticleSearch", ex.Message);
    }

    [Fact]
    public void Builder_FtsTable_WithCheckExtras_Throws()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.Table<ArticleSearch>()
                .Check(a => a.Title != "")
                .CreateTable());
    }

    [Fact]
    public void Builder_FtsTable_WithIndexExtras_Throws()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.Table<ArticleSearch>()
                .Index(a => a.Title)
                .CreateTable());
    }

    [Fact]
    public void Builder_Index_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<BookArchive>().Index<int>(b => b.Id + 1));
    }

    [Fact]
    public void Builder_Index_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<BookArchive>().Index(b => b.Title.Length));
    }

    [Fact]
    public void Builder_Computed_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<ProductLine>().Computed(p => p.Total + p.Price, p => p.Total));
    }

    [Fact]
    public void Builder_Index_DefaultName_UsesIdxConvention()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Index(b => b.AuthorId)
            .CreateTable();

        Assert.Contains("idx_BooksArchive_BookAuthorId", db.Schema.ListIndexes("BooksArchive"));
    }

    [Fact]
    public void Builder_Index_PartialFilter_EmitsWhereClause()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Index(b => b.Title, name: "IX_Partial", filter: b => b.Price > 0)
            .CreateTable();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Partial'");
        Assert.Contains("WHERE", indexSql);
        Assert.Contains("BookPrice", indexSql);
    }

    [Fact]
    public void Builder_Computed_DefaultStored_IsVirtual()
    {
        using TestDatabase db = new();

        db.Schema.Table<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity)
            .CreateTable();

        string tableSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");
        Assert.Contains("VIRTUAL", tableSql);
        Assert.DoesNotContain("STORED", tableSql);
    }

    [Fact]
    public void Builder_Check_NullCompare_NoParameters_RoundTrips()
    {
        using TestDatabase db = new();

        db.Schema.Table<BookArchive>()
            .Check(b => b.Title != null)
            .CreateTable();

        Assert.True(db.Schema.TableExists<BookArchive>());
    }

    [Fact]
    public void FormatLiteral_AllSupportedTypes_RenderExpected()
    {
        Assert.Equal("NULL", InvokeFormatLiteral(null));
        Assert.Equal("1", InvokeFormatLiteral(true));
        Assert.Equal("0", InvokeFormatLiteral(false));
        Assert.Equal("'a''b'", InvokeFormatLiteral("a'b"));
        Assert.Equal("5", InvokeFormatLiteral((byte)5));
        Assert.Equal("-5", InvokeFormatLiteral((sbyte)-5));
        Assert.Equal("100", InvokeFormatLiteral((short)100));
        Assert.Equal("100", InvokeFormatLiteral((ushort)100));
        Assert.Equal("123", InvokeFormatLiteral(123));
        Assert.Equal("123", InvokeFormatLiteral(123u));
        Assert.Equal("123", InvokeFormatLiteral(123L));
        Assert.Equal("123", InvokeFormatLiteral(123UL));
        Assert.Equal("1.5", InvokeFormatLiteral(1.5f));
        Assert.Equal("1.5", InvokeFormatLiteral(1.5));
        Assert.Equal("1.5", InvokeFormatLiteral(1.5m));
    }

    [Fact]
    public void FormatLiteral_UnsupportedType_ThrowsNotSupported()
    {
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            InvokeFormatLiteral(Guid.NewGuid()));
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void Builder_CompositePrimaryKey_EmitsTableLevelConstraint()
    {
        using TestDatabase db = new();

        db.Schema.Table<BuilderCompositeKeyEntity>()
            .Check(e => e.ProjectId > 0)
            .CreateTable();

        IReadOnlyList<SQLite.Framework.Models.SchemaColumnInfo> columns =
            db.Schema.ListColumns("BuilderCompositeKeyEntity");
        Assert.Equal(2, columns.Count(c => c.IsPrimaryKey));
        Assert.Contains(columns, c => c.Name == "ProjectId" && c.IsPrimaryKey);
        Assert.Contains(columns, c => c.Name == "TagId" && c.IsPrimaryKey);
    }

    private static string InvokeFormatLiteral(object? value)
    {
        MethodInfo method = typeof(SQLiteTableBuilder<BookArchive>)
            .GetMethod("FormatLiteral", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, new[] { value })!;
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("BuilderCompositeKeyEntity")]
file class BuilderCompositeKeyEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int ProjectId { get; set; }

    [System.ComponentModel.DataAnnotations.Key]
    public int TagId { get; set; }
}
