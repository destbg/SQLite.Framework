using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteTableSchemaTests
{
    [Fact]
    public void Model_AllFeaturesChained_Creates()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity, stored: true)
            .Check(p => p.Quantity >= 0, name: "CK_Qty")
            .Index(p => p.Price, name: "IX_Price"));
        db.Schema.CreateTable<ProductLine>();

        Assert.True(db.Schema.TableExists<ProductLine>());
        Assert.True(db.Schema.IndexExists("IX_Price"));

        db.Execute("INSERT INTO ProductLines (\"Id\", \"Price\", \"Quantity\") VALUES (1, 4.0, 2)");

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(8.0m, row.Total);

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO ProductLines (\"Id\", \"Price\", \"Quantity\") VALUES (2, 1.0, -5)"));
    }

    [Fact]
    public void Model_NullColumnExpression_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>().Index<string>(null!));
        Assert.Throws<ArgumentNullException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_NullCheckPredicate_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>().Check(null!));
        Assert.Throws<ArgumentNullException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_NullComputedExpression_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>().Computed(p => p.Total, null!));
        Assert.Throws<ArgumentNullException>(() => db.Schema.CreateTable<ProductLine>());
    }

    [Fact]
    public async Task Model_CreateAsync_RoundTrips()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity)
            .Index(p => p.Price, name: "IX_Price_Async"));

        await db.Schema.Table<ProductLine>().CreateTableAsync(TestContext.Current.CancellationToken);

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

        db.Execute(
            "INSERT INTO WithoutRowIdEntity (\"Code\", \"Name\") VALUES ('a', 'first')");
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

        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 9.99 });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 9.99 }));
    }

    [Fact]
    public void Model_CompositeIndex_CreatesOneIndexWithBothColumns()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, b.Price }, name: "IX_Builder_Composite"));
        db.Schema.CreateTable<BookArchive>();

        Assert.True(db.Schema.IndexExists("IX_Builder_Composite"));

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Builder_Composite'");
        Assert.Equal("CREATE INDEX \"IX_Builder_Composite\" ON \"BooksArchive\" (\"BookAuthorId\", \"BookPrice\")", indexSql);
        Assert.Equal("CREATE INDEX \"IX_Builder_Composite\" ON \"BooksArchive\" (\"BookAuthorId\", \"BookPrice\")", indexSql);
    }

    [Fact]
    public void Model_CompositeIndex_Unique_RejectsDuplicatePairs()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, b.Price }, name: "IX_Builder_Composite_Unique", unique: true));
        db.Schema.CreateTable<BookArchive>();

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "a", AuthorId = 1, Price = 1.0 });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "b", AuthorId = 1, Price = 1.0 }));
    }

    [Fact]
    public void Model_CompositeIndex_UnwrapsConvertOnArg()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { Boxed = (object?)b.AuthorId, b.Price }, name: "IX_Builder_Composite_Boxed"));
        db.Schema.CreateTable<BookArchive>();

        Assert.True(db.Schema.IndexExists("IX_Builder_Composite_Boxed"));
    }

    [Fact]
    public void Model_CompositeIndex_NotMemberAccessArg_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { Bad = b.AuthorId + 1, b.Price }));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_CompositeIndex_UnknownProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { Bad = b.Title.Length, b.Price }));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_FluentIndex_Unique_RejectsDuplicates()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId, name: "IX_BookArchive_Author_Unique", unique: true));
        db.Schema.CreateTable<BookArchive>();

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "a", AuthorId = 7, Price = 1 });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "b", AuthorId = 7, Price = 2 }));
    }

    [Fact]
    public void Model_Index_UnwrapsConvertOnColumnExpression()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index<object?>(b => b.Price, name: "IX_BoxedPrice"));
        db.Schema.CreateTable<BookArchive>();

        Assert.True(db.Schema.IndexExists("IX_BoxedPrice"));
    }

    [Fact]
    public void Model_Check_StringConstant_InlinesAsLiteral()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Title != ""));
        db.Schema.CreateTable<BookArchive>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Equal("CREATE TABLE \"BooksArchive\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, CHECK (\"BookTitle\" <> ''))", sql);
        Assert.Equal("CREATE TABLE \"BooksArchive\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, CHECK (\"BookTitle\" <> ''))", sql);
    }

    [Fact]
    public void Model_Check_DoubleConstant_InlinesAsLiteral()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Check(p => p.Quantity > 0)
            .Check(p => p.Price > 0.5m));
        db.Schema.CreateTable<ProductLine>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL, CHECK (\"Quantity\" > 0), CHECK (\"Price\" > 0.5))", sql);
        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL, CHECK (\"Quantity\" > 0), CHECK (\"Price\" > 0.5))", sql);
    }

    [Fact]
    public void Model_Check_StringWithApostrophe_DoublesQuote()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Title != "O'Reilly"));
        db.Schema.CreateTable<BookArchive>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Equal("CREATE TABLE \"BooksArchive\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, CHECK (\"BookTitle\" <> 'O''Reilly'))", sql);
    }

    [Fact]
    public void Model_Check_UntranslatableExpression_Throws()
    {
        Guid id = Guid.NewGuid();
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Title != id.ToString().Substring(0, 8) || b.AuthorId == 1));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Check_LongConstant_InlinesAsLiteral()
    {
        long limit = 1234567890123L;
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Check(p => (long)p.Quantity < limit));
        db.Schema.CreateTable<ProductLine>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL, CHECK (CAST(\"Quantity\" AS INTEGER) < 1234567890123))", sql);
    }

    [Fact]
    public void Model_Check_FloatConstant_InlinesAsLiteral()
    {
        float threshold = 2.5f;
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Check(p => (float)p.Price > threshold));
        db.Schema.CreateTable<ProductLine>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL, CHECK (CAST(\"Price\" AS REAL) > 2.5))", sql);
    }

    [Fact]
    public void Model_Check_BoolConstant_InlinesAsLiteral()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Check(p => (p.Quantity > 0) == true));
        db.Schema.CreateTable<ProductLine>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");

        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL, CHECK (\"Quantity\" > 0 = 1))", sql);
    }

    [Fact]
    public void Model_Check_DateTimeConstant_IsEnforced()
    {
        DateTime cutoff = new(2026, 1, 1);
        using ModelTestDatabase db = new(model => model.Entity<Article>()
            .Check(a => a.PublishedAt > cutoff));
        db.Schema.CreateTable<Article>();

        db.Table<Article>().Add(new Article { Title = "ok", Body = "b", PublishedAt = new DateTime(2026, 6, 1) });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<Article>().Add(new Article { Title = "bad", Body = "b", PublishedAt = new DateTime(2020, 1, 1) }));

        List<DateTime> stored = db.Table<Article>().Select(a => a.PublishedAt).ToList();
        Assert.Equal([new DateTime(2026, 6, 1)], stored);
    }

    [Fact]
    public void Model_NamedCheckConstraint_EmitsConstraintName()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Builder_Price"));
        db.Schema.CreateTable<BookArchive>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BooksArchive'");

        Assert.Equal("CREATE TABLE \"BooksArchive\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, CONSTRAINT \"CK_Builder_Price\" CHECK (\"BookPrice\" > 0.0))", sql);
        Assert.Equal("CREATE TABLE \"BooksArchive\" (\"BookId\" INTEGER PRIMARY KEY, \"BookTitle\" TEXT NOT NULL, \"BookAuthorId\" INTEGER NOT NULL, \"BookPrice\" REAL NOT NULL, CONSTRAINT \"CK_Builder_Price\" CHECK (\"BookPrice\" > 0.0))", sql);
    }

    [Fact]
    public void Model_FtsTable_WithComputedExtras_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<ArticleSearch>()
            .Computed(a => a.Title, a => a.Title));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<ArticleSearch>());
        Assert.Contains("ArticleSearch", ex.Message);
    }

    [Fact]
    public void Model_FtsTable_WithCheckExtras_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<ArticleSearch>()
            .Check(a => a.Title != ""));

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<ArticleSearch>());
    }

    [Fact]
    public void Model_FtsTable_WithIndexExtras_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<ArticleSearch>()
            .Index(a => a.Title));

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<ArticleSearch>());
    }

    [Fact]
    public void Model_Index_NotMemberAccess_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>().Index<int>(b => b.Id + 1));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Index_UnknownProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>().Index(b => b.Title.Length));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Computed_NotMemberAccess_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>().Computed(p => p.Total + p.Price, p => p.Total));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<ProductLine>());
    }

    [Fact]
    public void Model_Index_DefaultName_UsesIdxConvention()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId));
        db.Schema.CreateTable<BookArchive>();

        Assert.Contains("idx_BooksArchive_BookAuthorId", db.Schema.ListIndexes("BooksArchive"));
    }

    [Fact]
    public void Model_Index_SingleExpressionBody_EmitsExpressionIndex()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title.ToLower(), name: "IX_BookArchive_TitleLower"));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_TitleLower'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_TitleLower\" ON \"BooksArchive\" ((LOWER(\"BookTitle\")))", indexSql);
    }

    [Fact]
    public void Model_Index_ExpressionBody_QueryUsesIndex()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title.ToLower(), name: "IX_BookArchive_TitleLower"));
        db.Schema.CreateTable<BookArchive>();

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "FooBar", AuthorId = 1, Price = 1 });

        BookArchive row = db.Table<BookArchive>()
            .First(b => b.Title.ToLower() == "foobar");
        Assert.Equal("FooBar", row.Title);
    }

    [Fact]
    public void Model_Index_CompositeMixedColumnAndExpression_EmitsBothSlots()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, Lowered = b.Title.ToLower() }, name: "IX_BookArchive_AuthorAndTitleLower"));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorAndTitleLower'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorAndTitleLower\" ON \"BooksArchive\" (\"BookAuthorId\", (LOWER(\"BookTitle\")))", indexSql);
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorAndTitleLower\" ON \"BooksArchive\" (\"BookAuthorId\", (LOWER(\"BookTitle\")))", indexSql);
    }

    [Fact]
    public void Model_Index_ExpressionWithCollation_EmitsCollateClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title.Substring(0, 3), name: "IX_BookArchive_TitlePrefix", collation: SQLiteCollation.NoCase));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_TitlePrefix'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_TitlePrefix\" ON \"BooksArchive\" ((SUBSTR(\"BookTitle\", 0 + 1, 3)) COLLATE NOCASE)", indexSql);
    }

    [Fact]
    public void Model_Index_ExpressionWithoutName_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>().Index(b => b.Title.ToLower()));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Index_CompositeMixed_WithoutName_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, Lowered = b.Title.ToLower() }));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Index_SinglePlainNotMappedProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<IndexBuilderEntityWithNotMapped>().Index(b => b.NotPersisted));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<IndexBuilderEntityWithNotMapped>());
    }

    [Fact]
    public void Model_Index_CompositePlainNotMappedProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<IndexBuilderEntityWithNotMapped>().Index(b => new { b.Name, b.NotPersisted }));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<IndexBuilderEntityWithNotMapped>());
    }

    [Fact]
    public void Model_Default_NotMappedProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<IndexBuilderEntityWithNotMapped>().Default(b => b.NotPersisted, "x"));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<IndexBuilderEntityWithNotMapped>());
    }

    [Fact]
    public void Model_Index_DefaultDirection_EmitsNoClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId, name: "IX_BookArchive_AuthorDefault"));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorDefault'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorDefault\" ON \"BooksArchive\" (\"BookAuthorId\")", indexSql);
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorDefault\" ON \"BooksArchive\" (\"BookAuthorId\")", indexSql);
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorDefault\" ON \"BooksArchive\" (\"BookAuthorId\")", indexSql);
    }

    [Fact]
    public void Model_Index_ExplicitAscending_EmitsAscClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId, name: "IX_BookArchive_AuthorExplicitAsc", direction: SQLiteIndexDirection.Ascending));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorExplicitAsc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorExplicitAsc\" ON \"BooksArchive\" (\"BookAuthorId\" ASC)", indexSql);
    }

    [Fact]
    public void Model_Index_SingleDescending_EmitsDescClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId, name: "IX_BookArchive_AuthorDesc", direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorDesc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorDesc\" ON \"BooksArchive\" (\"BookAuthorId\" DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_CompositeAllDescending_AppliesToEverySlot()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, b.Title },
                name: "IX_BookArchive_AuthorAndTitleDesc",
                direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorAndTitleDesc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorAndTitleDesc\" ON \"BooksArchive\" (\"BookAuthorId\" DESC, \"BookTitle\" DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_CompositeMixedDirections_AppliesPerSlot()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, b.Title },
                name: "IX_BookArchive_AuthorAscTitleDesc",
                directions: [SQLiteIndexDirection.Ascending, SQLiteIndexDirection.Descending]));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_AuthorAscTitleDesc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_AuthorAscTitleDesc\" ON \"BooksArchive\" (\"BookAuthorId\" ASC, \"BookTitle\" DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_DirectionWithCollation_EmitsBothClauses()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title,
                name: "IX_BookArchive_TitleNoCaseDesc",
                collation: SQLiteCollation.NoCase,
                direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_TitleNoCaseDesc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_TitleNoCaseDesc\" ON \"BooksArchive\" (\"BookTitle\" COLLATE NOCASE DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_ExpressionWithDescending_EmitsDescAfterExpression()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title.ToLower(),
                name: "IX_BookArchive_TitleLowerDesc",
                direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_BookArchive_TitleLowerDesc'");
        Assert.Equal("CREATE INDEX \"IX_BookArchive_TitleLowerDesc\" ON \"BooksArchive\" ((LOWER(\"BookTitle\")) DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_DirectionsLengthMismatch_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => new { b.AuthorId, b.Title },
                name: "IX_Mismatch",
                directions: [SQLiteIndexDirection.Ascending]));
        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void Model_Index_InvalidDirectionEnum_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.AuthorId,
                name: "IX_InvalidDir",
                direction: (SQLiteIndexDirection)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Schema.CreateTable<BookArchive>());
    }

    [Fact]
    public void IndexedAttribute_Descending_EmitsDescClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<IndexAttrDirectionEntity>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_IndexAttrDirEntity_NameDesc'");
        Assert.Equal("CREATE INDEX \"IX_IndexAttrDirEntity_NameDesc\" ON \"IndexAttrDirectionEntity\" (\"Name\" DESC)", indexSql);
    }

    [Fact]
    public void Model_Index_PartialFilter_EmitsWhereClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Index(b => b.Title, name: "IX_Partial", filter: b => b.Price > 0));
        db.Schema.CreateTable<BookArchive>();

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Partial'");
        Assert.Equal("CREATE INDEX \"IX_Partial\" ON \"BooksArchive\" (\"BookTitle\") WHERE \"BookPrice\" > 0.0", indexSql);
        Assert.Equal("CREATE INDEX \"IX_Partial\" ON \"BooksArchive\" (\"BookTitle\") WHERE \"BookPrice\" > 0.0", indexSql);
    }

    [Fact]
    public void Model_Computed_DefaultStored_IsVirtual()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();

        string tableSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ProductLines'");
        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL GENERATED ALWAYS AS ((\"Price\" * CAST(\"Quantity\" AS REAL))) VIRTUAL)", tableSql);
        Assert.Equal("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL GENERATED ALWAYS AS ((\"Price\" * CAST(\"Quantity\" AS REAL))) VIRTUAL)", tableSql);
    }

    [Fact]
    public void Model_Check_NullCompare_NoParameters_RoundTrips()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Title != null));
        db.Schema.CreateTable<BookArchive>();

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
        Assert.Equal("'11112222-3333-4444-5555-666677778888'", InvokeFormatLiteral(new Guid("11112222-3333-4444-5555-666677778888")));
    }

    [Fact]
    public void FormatLiteral_UnsupportedType_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => InvokeFormatLiteral(new object()));
    }

    [Fact]
    public void Model_CompositePrimaryKey_EmitsTableLevelConstraint()
    {
        using ModelTestDatabase db = new(model => model.Entity<BuilderCompositeKeyEntity>()
            .Check(e => e.ProjectId > 0));
        db.Schema.CreateTable<BuilderCompositeKeyEntity>();

        IReadOnlyList<SQLite.Framework.Models.SchemaColumnInfo> columns =
            db.Schema.ListColumns("BuilderCompositeKeyEntity");
        Assert.Equal(2, columns.Count(c => c.IsPrimaryKey));
        Assert.Contains(columns, c => c.Name == "ProjectId" && c.IsPrimaryKey);
        Assert.Contains(columns, c => c.Name == "TagId" && c.IsPrimaryKey);
    }

    private static string InvokeFormatLiteral(object? value)
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("format-literal.db3").Build();
        return SQLite.Framework.Internals.Helpers.SqlLiteralHelper.FormatLiteral(value, options);
    }

    public class IndexBuilderEntityWithNotMapped
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }

        public required string Name { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string NotPersisted { get; set; } = "";
    }

    public class IndexAttrDirectionEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }

        [Indexed("IX_IndexAttrDirEntity_NameDesc", 0, Direction = SQLiteIndexDirection.Descending)]
        public required string Name { get; set; }
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
