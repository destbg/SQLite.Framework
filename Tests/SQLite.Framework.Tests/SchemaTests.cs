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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Book>();

        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void CreateTable_EmitsDeclaredIndexes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("Books");
        Assert.Contains("IX_Book_AuthorId", indexes);
    }

    [Fact]
    public void DropTable_Generic_RemovesTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.DropTable<Book>();

        Assert.False(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void DropTable_ByName_RemovesTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateIndex<Book>(b => b.Title, name: "IX_Book_Title");

        Assert.True(db.Schema.IndexExists("IX_Book_Title"));
    }

    [Fact]
    public void CreateIndex_DefaultName_UsesIdxConvention()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateIndex<Book>(b => b.Title);

        Assert.Contains("idx_Books_BookTitle", db.Schema.ListIndexes("Books"));
    }

    [Fact]
    public void CreateIndex_Unique_AddsUniqueConstraint()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();
        db.Schema.CreateIndex<BookArchive>(b => b.Title, name: "IX_BookArchive_Title_Unique", unique: true);

        db.Table<BookArchive>().Add(new BookArchive { Id = 1, Title = "same", AuthorId = 1, Price = 1 });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<BookArchive>().Add(new BookArchive { Id = 2, Title = "same", AuthorId = 2, Price = 2 }));
    }

    [Fact]
    public void CreateIndex_NullColumnExpression_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        Assert.Throws<ArgumentNullException>(() => db.Schema.CreateIndex<Book>(null!));
    }

    [Fact]
    public void DropIndex_RemovesIndex()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
        Assert.True(db.Schema.ColumnExists<Book>("BookTitle"));
    }

    [Fact]
    public void ColumnExists_Generic_ReturnsFalseForMissingColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        Assert.False(db.Schema.ColumnExists<Book>("NoSuchColumn"));
    }

    [Fact]
    public void ListTables_ReturnsCreatedTables()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();

        IReadOnlyList<string> tables = db.Schema.ListTables();

        Assert.Contains("Books", tables);
        Assert.Contains("BooksArchive", tables);
    }

    [Fact]
    public void ListIndexes_NoFilter_ReturnsAllIndexes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        IReadOnlyList<string> indexes = db.Schema.ListIndexes();

        Assert.Contains("IX_Book_AuthorId", indexes);
    }

    [Fact]
    public void ListIndexes_TableFilter_ReturnsOnlyIndexesForThatTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();
        db.Schema.CreateIndex<BookArchive>(b => b.Title, name: "IX_Archive_Title");

        IReadOnlyList<string> archiveIndexes = db.Schema.ListIndexes("BooksArchive");

        Assert.Contains("IX_Archive_Title", archiveIndexes);
        Assert.DoesNotContain("IX_Book_AuthorId", archiveIndexes);
    }

    [Fact]
    public void ListColumns_Generic_ReturnsMappedColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();

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
        db.Schema.CreateTable<EvolvingTable>();

        db.Schema.AddColumn<EvolvingTablePlusName>(nameof(EvolvingTablePlusName.Name));

        IReadOnlyList<SchemaColumnInfo> columns = db.Schema.ListColumns<EvolvingTable>();
        Assert.Contains(columns, c => c.Name == "Name");
    }

    [Fact]
    public void AddColumn_UnknownProperty_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<InvalidOperationException>(() => db.Schema.AddColumn<Book>("NotAProperty"));
    }

    [Fact]
    public void RenameColumn_RenamesInDatabase()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();

        db.Schema.RenameColumn<BookArchive>("BookTitle", "Title2");

        Assert.True(db.Schema.ColumnExists<BookArchive>("Title2"));
        Assert.False(db.Schema.ColumnExists<BookArchive>("BookTitle"));
    }

    [Fact]
    public void DropColumn_RemovesTheColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();

        db.Schema.DropColumn<BookArchive>("BookPrice");

        Assert.False(db.Schema.ColumnExists<BookArchive>("BookPrice"));
    }

    [Fact]
    public void RenameTable_RenamesInDatabase()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();

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
        db.Schema.CreateTable<Book>();

#pragma warning disable CS0618
        db.Table<Book>().DropTable();
#pragma warning restore CS0618

        Assert.False(db.Schema.TableExists<Book>());
    }
}
