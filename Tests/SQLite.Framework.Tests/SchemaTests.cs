using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaTests
{
    [Fact]
    public void CreateTable_Generic_CreatesTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void CreateTable_NonGeneric_CreatesTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable(typeof(Book));

        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void CreateTable_IsIdempotent()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void CreateTable_EmitsDeclaredIndexes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("Books");
        Assert.Contains("IX_Book_AuthorId", indexes);
    }

    [Fact]
    public void CreateTable_CompositeIndex_CreatesOneIndexWithBothColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeIndexedEntity>();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("CompositeIndexedTable");
        Assert.Single(indexes, n => n == "IX_Schema_Composite");

        string indexSql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Schema_Composite'");
        Assert.Contains("Col1", indexSql);
        Assert.Contains("Col2", indexSql);
    }

    [Fact]
    public void DropTable_Generic_RemovesTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.DropTable<Book>();

        Assert.False(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void DropTable_ByName_RemovesTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.DropTable("Books");

        Assert.False(db.Schema.TableExists("Books"));
    }

    [Fact]
    public void DropTable_NonExistent_DoesNotThrow()
    {
        using TestDatabase db = new();
        db.Schema.DropTable("NotThere");
        Assert.False(db.Schema.TableExists("NotThere"));
    }

    [Fact]
    public void TableExists_Returns_FalseForMissingTable()
    {
        using TestDatabase db = new();
        Assert.False(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void TableExists_NullName_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() => db.Schema.TableExists(null!));
    }

    [Fact]
    public void CreateIndex_OverColumn_EmitsIndex()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateIndex<Book>(b => b.Title, name: "IX_Book_Title");

        Assert.True(db.Schema.IndexExists("IX_Book_Title"));
    }

    [Fact]
    public void CreateIndex_DefaultName_UsesIdxConvention()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateIndex<Book>(b => b.Title);

        Assert.Contains("idx_Books_BookTitle", db.Schema.ListIndexes("Books"));
    }

    [Fact]
    public void CreateIndex_Unique_AddsUniqueConstraint()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();
        db.Schema.CreateIndex<BookArchive>(b => b.Title, name: "IX_BookArchive_Title_Unique", unique: true);

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "same", AuthorId = 1, Price = 1 });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "same", AuthorId = 2, Price = 2 }));
    }

    [Fact]
    public void CreateIndex_NullColumnExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Assert.Throws<ArgumentNullException>(() => db.Schema.CreateIndex<Book>(null!));
    }

    [Fact]
    public void DropIndex_RemovesIndex()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateIndex<Book>(b => b.Title, name: "IX_Drop_Me");

        db.Schema.DropIndex("IX_Drop_Me");

        Assert.False(db.Schema.IndexExists("IX_Drop_Me"));
    }

    [Fact]
    public void DropIndex_NonExistent_DoesNotThrow()
    {
        using TestDatabase db = new();
        db.Schema.DropIndex("NotThere");
    }

    [Fact]
    public void IndexExists_Returns_FalseForMissingIndex()
    {
        using TestDatabase db = new();
        Assert.False(db.Schema.IndexExists("NotThere"));
    }

    [Fact]
    public void ColumnExists_Generic_ReturnsTrueForMappedColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Assert.True(db.Schema.ColumnExists<Book>("BookTitle"));
    }

    [Fact]
    public void ColumnExists_Generic_ReturnsFalseForMissingColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Assert.False(db.Schema.ColumnExists<Book>("NoSuchColumn"));
    }

    [Fact]
    public void ListTables_ReturnsCreatedTables()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookArchive>().Schema.CreateTable();

        IReadOnlyList<string> tables = db.Schema.ListTables();

        Assert.Contains("Books", tables);
        Assert.Contains("BooksArchive", tables);
    }

    [Fact]
    public void ListIndexes_NoFilter_ReturnsAllIndexes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes();

        Assert.Contains("IX_Book_AuthorId", indexes);
    }

    [Fact]
    public void ListIndexes_TableFilter_ReturnsOnlyIndexesForThatTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookArchive>().Schema.CreateTable();
        db.Schema.CreateIndex<BookArchive>(b => b.Title, name: "IX_Archive_Title");

        IReadOnlyList<string> archiveIndexes = db.Schema.ListIndexes("BooksArchive");

        Assert.Contains("IX_Archive_Title", archiveIndexes);
        Assert.DoesNotContain("IX_Book_AuthorId", archiveIndexes);
    }

    [Fact]
    public void ListColumns_Generic_ReturnsMappedColumns()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();

        IReadOnlyList<SchemaColumnInfo> columns = db.Schema.ListColumns<BookArchive>();

        Assert.Equal(4, columns.Count);
        Assert.Contains(columns, c => c.Name == "BookId" && c.IsPrimaryKey);
        Assert.Contains(columns, c => c.Name == "BookTitle" && !c.IsNullable);
        Assert.Contains(columns, c => c.Name == "BookPrice");
    }

    [Fact]
    public void AddColumn_AddsTheColumnToTheTable()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingTablePlusName>(nameof(EvolvingTablePlusName.Name));

        IReadOnlyList<SchemaColumnInfo> columns = db.Schema.ListColumns<EvolvingTable>();
        Assert.Contains(columns, c => c.Name == "Name");
    }

    [Fact]
    public void AddColumn_WithDefaultValue_BackfillsExistingRows()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");
        db.Execute("INSERT INTO Evolving (Id) VALUES (2)");

        db.Schema.AddColumn<EvolvingPlusRequiredCount>(nameof(EvolvingPlusRequiredCount.Count), defaultValue: 7);

        IReadOnlyList<int> counts = db.Query<int>("SELECT Count FROM Evolving ORDER BY Id");
        Assert.Equal(new[] { 7, 7 }, counts);
    }

    [Fact]
    public void AddColumn_DefaultValue_EmitsDefaultClauseInSchema()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredCount>(nameof(EvolvingPlusRequiredCount.Count), defaultValue: 42);

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("DEFAULT 42", sql);
    }

    [Fact]
    public void AddColumn_StringDefault_EscapesQuote()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredLabel>(nameof(EvolvingPlusRequiredLabel.Label), defaultValue: "O'Reilly");

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("'O''Reilly'", sql);
    }

    [Fact]
    public void AddColumn_PropertySelector_AddsTheColumn()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingTablePlusName>(e => e.Name);

        Assert.True(db.Schema.ColumnExists<EvolvingTablePlusName>("Name"));
    }

    [Fact]
    public void AddColumn_PropertySelector_WithDefault_BackfillsExistingRows()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        db.Schema.AddColumn<EvolvingPlusRequiredCount>(e => e.Count, defaultValue: 5);

        Assert.Equal(5, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void AddColumn_PropertySelector_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.AddColumn<EvolvingPlusRequiredCount>(e => e.Count + 1));
    }

    [Fact]
    public async Task AddColumnAsync_PropertySelector_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);

        await db.Schema.AddColumnAsync<EvolvingTablePlusName>(
            e => e.Name,
            TestContext.Current.CancellationToken);

        Assert.True(db.Schema.ColumnExists<EvolvingTablePlusName>("Name"));
    }

    [Fact]
    public async Task AddColumnAsync_PropertySelector_WithDefault_BackfillsExistingRows()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredCount>(
            e => e.Count,
            defaultValue: 13,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(13, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void AddColumn_NoDefault_NullableColumn_WorksOnPopulatedTable()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        db.Schema.AddColumn<EvolvingTablePlusName>(nameof(EvolvingTablePlusName.Name));

        Assert.True(db.Schema.ColumnExists<EvolvingTablePlusName>("Name"));
    }

    [Fact]
    public async Task AddColumnAsync_WithDefaultValue_BackfillsExistingRows()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredCount>(
            nameof(EvolvingPlusRequiredCount.Count),
            defaultValue: 99,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(99, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_CurrentTimestamp_EmitsKeyword()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredLabel>(nameof(EvolvingPlusRequiredLabel.Label), SQLiteColumnDefault.CurrentTimestamp);

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("DEFAULT CURRENT_TIMESTAMP", sql);
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_OnEmptyTable_AppliesToNewInserts()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredLabel>(e => e.Label, SQLiteColumnDefault.CurrentDate);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        string label = db.QueryFirst<string>("SELECT Label FROM Evolving");
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", label);
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_OnPopulatedTable_Throws()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        Assert.Throws<SQLiteException>(() =>
            db.Schema.AddColumn<EvolvingPlusRequiredLabel>(e => e.Label, SQLiteColumnDefault.CurrentTimestamp));
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_PropertySelector_EmitsKeyword()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredLabel>(e => e.Label, SQLiteColumnDefault.CurrentTime);

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("DEFAULT CURRENT_TIME", sql);
    }

    [Fact]
    public void AddColumn_ExpressionDefault_BackfillsWithConstant()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        db.Schema.AddColumn<EvolvingPlusRequiredCount>(nameof(EvolvingPlusRequiredCount.Count), () => 10 + 5);

        Assert.Equal(15, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void AddColumn_ExpressionDefault_PropertySelector_BackfillsWithConstant()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        db.Schema.AddColumn<EvolvingPlusRequiredLabel>(e => e.Label, () => "hello " + "world");

        Assert.Equal("hello world", db.QueryFirst<string>("SELECT Label FROM Evolving"));
    }

    [Fact]
    public void AddColumn_ExpressionDefault_EmitsParenthesizedClause()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Schema.AddColumn<EvolvingPlusRequiredCount>(e => e.Count, () => 7 * 6);

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("DEFAULT (", sql);
        Assert.Contains("42", sql);
    }

    [Fact]
    public async Task AddColumnAsync_SQLiteColumnDefault_PropertySelector_AppliesToNewInserts()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredLabel>(
            e => e.Label,
            SQLiteColumnDefault.CurrentTimestamp,
            TestContext.Current.CancellationToken);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        string label = db.QueryFirst<string>("SELECT Label FROM Evolving");
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", label);
    }

    [Fact]
    public async Task AddColumnAsync_ExpressionDefault_PropertySelector_BackfillsRows()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredCount>(
            e => e.Count,
            () => 100 - 1,
            TestContext.Current.CancellationToken);

        Assert.Equal(99, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void AddColumn_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Schema.AddColumn<Book>("NotAProperty"));
    }

    [Fact]
    public void RenameColumn_RenamesInDatabase()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();

        db.Schema.RenameColumn<BookArchive>("BookTitle", "Title2");

        Assert.True(db.Schema.ColumnExists<BookArchive>("Title2"));
        Assert.False(db.Schema.ColumnExists<BookArchive>("BookTitle"));
    }

    [Fact]
    public void DropColumn_RemovesTheColumn()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();

        db.Schema.DropColumn<BookArchive>("BookPrice");

        Assert.False(db.Schema.ColumnExists<BookArchive>("BookPrice"));
    }

    [Fact]
    public void RenameTable_RenamesInDatabase()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();

        db.Schema.RenameTable<BookArchive>("RenamedArchive");

        Assert.True(db.Schema.TableExists("RenamedArchive"));
        Assert.False(db.Schema.TableExists("BooksArchive"));
    }

    [Fact]
    public async Task CreateTableAsync_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.TableExistsAsync<Book>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropTableAsync_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.DropTableAsync<Book>(TestContext.Current.CancellationToken);
        Assert.False(await db.Schema.TableExistsAsync<Book>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListColumnsAsync_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<BookArchive>(TestContext.Current.CancellationToken);
        IReadOnlyList<SchemaColumnInfo> cols = await db.Schema.ListColumnsAsync<BookArchive>(TestContext.Current.CancellationToken);
        Assert.Equal(4, cols.Count);
    }

    [Fact]
    public async Task CreateTableAsync_NonGeneric_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync(typeof(Book), TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.TableExistsAsync<Book>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropTableAsync_ByName_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.DropTableAsync("Books", TestContext.Current.CancellationToken);
        Assert.False(await db.Schema.TableExistsAsync("Books", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateIndexAsync_OverColumn_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.CreateIndexAsync<Book>(b => b.Title, "IX_Book_Title", false, TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.IndexExistsAsync("IX_Book_Title", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropIndexAsync_RemovesIndex()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.CreateIndexAsync<Book>(b => b.Title, "IX_Book_Title2", false, TestContext.Current.CancellationToken);
        await db.Schema.DropIndexAsync("IX_Book_Title2", TestContext.Current.CancellationToken);
        Assert.False(await db.Schema.IndexExistsAsync("IX_Book_Title2", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ColumnExistsAsync_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.ColumnExistsAsync<Book>("BookTitle", TestContext.Current.CancellationToken));
        Assert.False(await db.Schema.ColumnExistsAsync<Book>("Missing", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListTablesAsync_ReturnsCreatedTables()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        IReadOnlyList<string> tables = await db.Schema.ListTablesAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Books", tables);
    }

    [Fact]
    public async Task ListIndexesAsync_RoundTrips()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.CreateIndexAsync<Book>(b => b.Title, "IX_Book_TitleAsync", false, TestContext.Current.CancellationToken);
        IReadOnlyList<string> all = await db.Schema.ListIndexesAsync(null, TestContext.Current.CancellationToken);
        Assert.Contains("IX_Book_TitleAsync", all);
        IReadOnlyList<string> forBook = await db.Schema.ListIndexesAsync("Books", TestContext.Current.CancellationToken);
        Assert.Contains("IX_Book_TitleAsync", forBook);
    }

    [Fact]
    public async Task AddColumnAsync_AddsTheColumn()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<BookArchive>(TestContext.Current.CancellationToken);
        await db.Schema.DropColumnAsync<BookArchive>("BookPrice", TestContext.Current.CancellationToken);
        await db.Schema.AddColumnAsync<BookArchive>(nameof(BookArchive.Price), TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.ColumnExistsAsync<BookArchive>("BookPrice", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenameColumnAsync_RenamesInDatabase()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<BookArchive>(TestContext.Current.CancellationToken);
        await db.Schema.RenameColumnAsync<BookArchive>("BookTitle", "Title2", TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.ColumnExistsAsync<BookArchive>("Title2", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropColumnAsync_RemovesTheColumn()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<BookArchive>(TestContext.Current.CancellationToken);
        await db.Schema.DropColumnAsync<BookArchive>("BookPrice", TestContext.Current.CancellationToken);
        Assert.False(await db.Schema.ColumnExistsAsync<BookArchive>("BookPrice", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenameTableAsync_RenamesInDatabase()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<Book>(TestContext.Current.CancellationToken);
        await db.Schema.RenameTableAsync<Book>("Books_Renamed", TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.TableExistsAsync("Books_Renamed", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TableBuilder_CreateTableAsync_RoundTrips()
    {
        using TestDatabase db = new();
        SQLiteTableBuilder<Book> builder = db.Schema.Table<Book>();
        await builder.CreateTableAsync(TestContext.Current.CancellationToken);
        Assert.True(await db.Schema.TableExistsAsync<Book>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void UseSchema_RegistersCustomFactory()
    {
        bool factoryCalled = false;

        using TestDatabase db = new(b => b.UseSchema(database =>
        {
            factoryCalled = true;
            return new SQLiteSchema(database);
        }));

        _ = db.Schema;

        Assert.True(factoryCalled);
    }

    [Fact]
    public void UseSchema_NullFactory_Throws()
    {
        SQLiteOptionsBuilder builder = new("test.db3");
        Assert.Throws<ArgumentNullException>(() => builder.UseSchema(null!));
    }

    [Fact]
    public void ObsoleteShim_CreateTable_StillWorks()
    {
        using TestDatabase db = new();

#pragma warning disable CS0618
        db.Table<Book>().CreateTable();
#pragma warning restore CS0618

        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void ObsoleteShim_DropTable_StillWorks()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

#pragma warning disable CS0618
        db.Table<Book>().DropTable();
#pragma warning restore CS0618

        Assert.False(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void CompositePrimaryKey_CreateTable_EmitsTableLevelConstraint()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeKeyEntity>();

        IReadOnlyList<SchemaColumnInfo> columns = db.Schema.ListColumns("CompositeKeyEntity");
        Assert.Equal(2, columns.Count(c => c.IsPrimaryKey));
        Assert.Contains(columns, c => c.Name == "ProjectId" && c.IsPrimaryKey);
        Assert.Contains(columns, c => c.Name == "TagId" && c.IsPrimaryKey);
    }

    [Fact]
    public void CompositePrimaryKey_AddRemove_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeKeyEntity>();

        SQLiteTable<CompositeKeyEntity> table = db.Table<CompositeKeyEntity>();
        table.Add(new CompositeKeyEntity { ProjectId = 1, TagId = 10, Note = "a" });
        table.Add(new CompositeKeyEntity { ProjectId = 1, TagId = 20, Note = "b" });
        table.Add(new CompositeKeyEntity { ProjectId = 2, TagId = 10, Note = "c" });

        Assert.Equal(3, table.Count());

        table.Remove(new CompositeKeyEntity { ProjectId = 1, TagId = 10, Note = "ignored" });

        Assert.Equal(2, table.Count());
        Assert.DoesNotContain(table.ToList(), e => e.ProjectId == 1 && e.TagId == 10);
    }

    [Fact]
    public void CompositePrimaryKey_Update_TargetsBothColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeKeyEntity>();

        SQLiteTable<CompositeKeyEntity> table = db.Table<CompositeKeyEntity>();
        table.Add(new CompositeKeyEntity { ProjectId = 1, TagId = 10, Note = "before-1-10" });
        table.Add(new CompositeKeyEntity { ProjectId = 1, TagId = 20, Note = "before-1-20" });

        table.Update(new CompositeKeyEntity { ProjectId = 1, TagId = 20, Note = "after-1-20" });

        Assert.Equal("before-1-10", table.Single(e => e.TagId == 10).Note);
        Assert.Equal("after-1-20", table.Single(e => e.TagId == 20).Note);
    }

    [Fact]
    public void CreateTable_WithoutRowId_AppendsClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WithoutRowIdEntity>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'WithoutRowIdEntity'");
        Assert.Contains("WITHOUT ROWID", sql);
    }

    [Fact]
    public void CreateIndex_OverIntColumn_UnwrapsConvert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        db.Schema.CreateIndex<Book>(b => b.AuthorId, name: "IX_Boxed_Author");

        Assert.True(db.Schema.IndexExists("IX_Boxed_Author"));
    }

    [Fact]
    public void CreateIndex_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateIndex<Book>(b => b.AuthorId + 1));
    }

    [Fact]
    public void CreateIndex_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateIndex<Book>(b => b.Title.Length));
    }

    [Fact]
    public void DropTable_FtsTableWithTriggers_DropsTriggersThenTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        Assert.True(db.Schema.TableExists<ArticleSearch>());
        Assert.Contains("ArticleSearch_sync_ai", db.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'trigger'"));

        db.Schema.DropTable<ArticleSearch>();

        Assert.False(db.Schema.TableExists<ArticleSearch>());
        Assert.DoesNotContain("ArticleSearch_sync_ai", db.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'trigger'"));
    }

    [Fact]
    public void CreateTable_FtsContentless_EmitsContentClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<ContentlessSearch>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ContentlessSearch'");
        Assert.Contains("content=''", sql);
    }

    [Fact]
    public void CreateTable_FtsWithPrefix_EmitsPrefixClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<PrefixSearch>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'PrefixSearch'");
        Assert.Contains("prefix='2 3'", sql);
    }

    [Fact]
    public void CreateTable_FtsWithExplicitRowIdColumn_UsesIt()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ExplicitRowIdSearch>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ExplicitRowIdSearch'");
        Assert.Contains("content_rowid='Id'", sql);
    }

    [Fact]
    public void CreateTable_FtsWithCompositePkSource_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeKeyEntity>();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<CompositePkFtsSearch>());
        Assert.Contains("more than one primary key", ex.Message);
    }

    [Fact]
    public void CreateTable_DefaultValueAttribute_EmitsDefaultClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultRating>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithDefaultRating'");
        Assert.Contains("DEFAULT 10", sql);
    }

    [Fact]
    public void Add_DefaultValueAttribute_OmitsColumnWhenClrDefault()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultRating>();

        SQLiteTable<BookWithDefaultRating> table = db.Table<BookWithDefaultRating>();
        BookWithDefaultRating book = new() { Title = "Hello" };
        table.Add(book);

        int rating = db.QueryFirst<int>("SELECT Rating FROM BookWithDefaultRating");
        Assert.Equal(10, rating);
    }

    [Fact]
    public void Add_DefaultValueAttribute_BindsColumnWhenExplicit()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultRating>();

        SQLiteTable<BookWithDefaultRating> table = db.Table<BookWithDefaultRating>();
        table.Add(new BookWithDefaultRating { Title = "Hello", Rating = 3 });

        int rating = db.QueryFirst<int>("SELECT Rating FROM BookWithDefaultRating");
        Assert.Equal(3, rating);
    }

    [Fact]
    public void AddRange_DefaultValueAttribute_AppliesPerRow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultRating>();

        SQLiteTable<BookWithDefaultRating> table = db.Table<BookWithDefaultRating>();
        table.AddRange(
        [
            new BookWithDefaultRating { Title = "A", Rating = 7 },
            new BookWithDefaultRating { Title = "B" },
            new BookWithDefaultRating { Title = "C", Rating = 9 },
        ]);

        int[] ratings = [.. db.Query<int>("SELECT Rating FROM BookWithDefaultRating ORDER BY Id")];
        Assert.Equal([7, 10, 9], ratings);
    }

    [Fact]
    public void AddOrUpdate_DefaultValueAttribute_OmitsDefaultColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultRating>();

        SQLiteTable<BookWithDefaultRating> table = db.Table<BookWithDefaultRating>();
        table.AddOrUpdate(new BookWithDefaultRating { Id = 1, Title = "First" }, SQLiteConflict.Replace);

        int rating = db.QueryFirst<int>("SELECT Rating FROM BookWithDefaultRating WHERE Id = 1");
        Assert.Equal(10, rating);
    }

    [Fact]
    public void CreateTable_BuilderDefaultLiteral_EmitsDefaultClause()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderDefault>()
            .Default(b => b.Rating, 25)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderDefault'");
        Assert.Contains("DEFAULT 25", sql);
    }

    [Fact]
    public void Add_BuilderDefaultLiteral_OmitsColumnWhenClrDefault()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderDefault>()
            .Default(b => b.Rating, 25)
            .CreateTable();

        SQLiteTable<BookWithBuilderDefault> table = db.Table<BookWithBuilderDefault>();
        table.Add(new BookWithBuilderDefault { Title = "Hello" });

        int rating = db.QueryFirst<int>("SELECT Rating FROM BookWithBuilderDefault");
        Assert.Equal(25, rating);
    }

    [Fact]
    public void CreateTable_BuilderDefaultKeyword_EmitsCurrentTimestamp()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderTimestamp>()
            .Default(b => b.CreatedAt, SQLiteColumnDefault.CurrentTimestamp)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderTimestamp'");
        Assert.Contains("DEFAULT CURRENT_TIMESTAMP", sql);
    }

    [Fact]
    public void Add_BuilderDefaultKeyword_AppliesDatabaseDefault()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderTimestamp>()
            .Default(b => b.CreatedAt, SQLiteColumnDefault.CurrentTimestamp)
            .CreateTable();

        SQLiteTable<BookWithBuilderTimestamp> table = db.Table<BookWithBuilderTimestamp>();
        table.Add(new BookWithBuilderTimestamp { Title = "Hello" });

        string createdAt = db.QueryFirst<string>("SELECT CreatedAt FROM BookWithBuilderTimestamp");
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", createdAt);
    }

    [Fact]
    public void CreateTable_BuilderDefaultExpression_EmitsParenthesizedClause()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderDefault>()
            .Default(b => b.Rating, () => 6 * 7)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderDefault'");
        Assert.Contains("DEFAULT (", sql);
        Assert.Contains("42", sql);
    }

    [Fact]
    public void Add_BuilderDefaultExpression_AppliesDatabaseDefault()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderDefault>()
            .Default(b => b.Rating, () => 6 * 7)
            .CreateTable();

        SQLiteTable<BookWithBuilderDefault> table = db.Table<BookWithBuilderDefault>();
        table.Add(new BookWithBuilderDefault { Title = "Hello" });

        int rating = db.QueryFirst<int>("SELECT Rating FROM BookWithBuilderDefault");
        Assert.Equal(42, rating);
    }

    [Fact]
    public void CreateTable_BuilderDefault_CurrentTime_EmitsKeyword()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderTimestamp>()
            .Default(b => b.CreatedAt, SQLiteColumnDefault.CurrentTime)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderTimestamp'");
        Assert.Contains("DEFAULT CURRENT_TIME", sql);
    }

    [Fact]
    public void CreateTable_BuilderDefault_CurrentDate_EmitsKeyword()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderTimestamp>()
            .Default(b => b.CreatedAt, SQLiteColumnDefault.CurrentDate)
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderTimestamp'");
        Assert.Contains("DEFAULT CURRENT_DATE", sql);
    }

    [Fact]
    public void BuilderDefault_InvalidEnum_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Schema.Table<BookWithBuilderTimestamp>().Default(b => b.CreatedAt, (SQLiteColumnDefault)999));
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_InvalidEnum_Throws()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Schema.AddColumn<EvolvingPlusRequiredLabel>(nameof(EvolvingPlusRequiredLabel.Label), (SQLiteColumnDefault)999));
    }

    [Fact]
    public void AddColumn_SQLiteColumnDefault_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.AddColumn<EvolvingPlusRequiredLabel>("NotAProperty", SQLiteColumnDefault.CurrentTimestamp));
    }

    [Fact]
    public void AddColumn_ExpressionDefault_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.AddColumn<EvolvingPlusRequiredCount>("NotAProperty", () => 5));
    }

    [Fact]
    public async Task AddColumnAsync_StringPropertyName_SQLiteColumnDefault_EmitsKeyword()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredLabel>(
            nameof(EvolvingPlusRequiredLabel.Label),
            SQLiteColumnDefault.CurrentDate,
            TestContext.Current.CancellationToken);

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Evolving'");
        Assert.Contains("DEFAULT CURRENT_DATE", sql);
    }

    [Fact]
    public async Task AddColumnAsync_StringPropertyName_ExpressionDefault_EmitsClause()
    {
        using TestDatabase db = new();
        await db.Schema.CreateTableAsync<EvolvingTable>(TestContext.Current.CancellationToken);
        db.Execute("INSERT INTO Evolving (Id) VALUES (1)");

        await db.Schema.AddColumnAsync<EvolvingPlusRequiredCount>(
            nameof(EvolvingPlusRequiredCount.Count),
            () => 5 + 5,
            TestContext.Current.CancellationToken);

        Assert.Equal(10, db.QueryFirst<int>("SELECT Count FROM Evolving"));
    }

    [Fact]
    public void Add_DefaultColumnFollowedByNonDefault_OmitsOnlyDefault()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookWithDefaultFirst>();

        SQLiteTable<BookWithDefaultFirst> table = db.Table<BookWithDefaultFirst>();
        table.Add(new BookWithDefaultFirst { Title = "Hello" });

        BookWithDefaultFirst row = db.QueryFirst<BookWithDefaultFirst>("SELECT * FROM BookWithDefaultFirst");
        Assert.Equal(10, row.Rating);
        Assert.Equal("Hello", row.Title);
    }

    [Fact]
    public void BuilderDefault_UntranslatableExpression_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() =>
            db.Schema.Table<BookWithBuilderTimestamp>()
                .Default(b => b.CreatedAt, () => new string('a', 3)));
    }

    [Fact]
    public void BuilderDefault_ParameterlessSqlFunction_TranslatesWithoutParameters()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookWithBuilderTimestamp>()
            .Default(b => b.CreatedAt, () => SQLiteFunctions.SqliteVersion())
            .CreateTable();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'BookWithBuilderTimestamp'");
        Assert.Contains("sqlite_version()", sql);
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("BookWithDefaultRating")]
file class BookWithDefaultRating
{
    [System.ComponentModel.DataAnnotations.Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [System.ComponentModel.DefaultValue(10)]
    public int Rating { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("BookWithBuilderDefault")]
file class BookWithBuilderDefault
{
    [System.ComponentModel.DataAnnotations.Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    public int Rating { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("BookWithDefaultFirst")]
file class BookWithDefaultFirst
{
    [System.ComponentModel.DataAnnotations.Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    [System.ComponentModel.DefaultValue(10)]
    public int Rating { get; set; }

    public required string Title { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("BookWithBuilderTimestamp")]
file class BookWithBuilderTimestamp
{
    [System.ComponentModel.DataAnnotations.Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? CreatedAt { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("ContentlessSearch")]
[SQLite.Framework.Attributes.FullTextSearch(ContentMode = SQLite.Framework.Enums.FtsContentMode.Contentless)]
file class ContentlessSearch
{
    [SQLite.Framework.Attributes.FullTextRowId]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.FullTextIndexed]
    public required string Body { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("PrefixSearch")]
[SQLite.Framework.Attributes.FullTextSearch(ContentMode = SQLite.Framework.Enums.FtsContentMode.Internal, Prefix = "2 3")]
file class PrefixSearch
{
    [SQLite.Framework.Attributes.FullTextRowId]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.FullTextIndexed]
    public required string Body { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("ExplicitRowIdSearch")]
[SQLite.Framework.Attributes.FullTextSearch(
    ContentMode = SQLite.Framework.Enums.FtsContentMode.External,
    ContentTable = typeof(SQLite.Framework.Tests.Entities.Article),
    ContentRowIdColumn = "Id",
    AutoSync = SQLite.Framework.Enums.FtsAutoSync.Manual)]
file class ExplicitRowIdSearch
{
    [SQLite.Framework.Attributes.FullTextRowId]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.FullTextIndexed]
    public required string Title { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("CompositePkFtsSearch")]
[SQLite.Framework.Attributes.FullTextSearch(
    ContentMode = SQLite.Framework.Enums.FtsContentMode.External,
    ContentTable = typeof(CompositeKeyEntity),
    AutoSync = SQLite.Framework.Enums.FtsAutoSync.Manual)]
file class CompositePkFtsSearch
{
    [SQLite.Framework.Attributes.FullTextRowId]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.FullTextIndexed]
    public required string Note { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("CompositeKeyEntity")]
file class CompositeKeyEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int ProjectId { get; set; }

    [System.ComponentModel.DataAnnotations.Key]
    public int TagId { get; set; }

    public string Note { get; set; } = string.Empty;
}

[System.ComponentModel.DataAnnotations.Schema.Table("Evolving")]
file class EvolvingPlusRequiredCount
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public int Count { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("Evolving")]
file class EvolvingPlusRequiredLabel
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public required string Label { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("CompositeIndexedTable")]
file class CompositeIndexedEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed("IX_Schema_Composite", 0)]
    public int Col1 { get; set; }

    [SQLite.Framework.Attributes.Indexed("IX_Schema_Composite", 1)]
    public string? Col2 { get; set; }
}
