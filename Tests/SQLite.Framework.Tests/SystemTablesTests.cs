using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SystemTablesTests
{
    [Fact]
    public void Master_LinqWhere_ReturnsCreatedTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<SQLiteMaster> rows = db.Pragmas.Master
            .Where(m => m.Type == "table" && m.Name == "Books")
            .ToList();

        Assert.Single(rows);
        Assert.Equal("Books", rows[0].Name);
        Assert.Equal("Books", rows[0].TableName);
        Assert.NotNull(rows[0].Sql);
    }

    [Fact]
    public void Master_Select_ProjectsToScalar()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        List<string> tableNames = db.Pragmas.Master
            .Where(m => m.Type == "table")
            .OrderBy(m => m.Name)
            .Select(m => m.Name)
            .ToList();

        Assert.Contains("Authors", tableNames);
        Assert.Contains("Books", tableNames);
    }

    [Fact]
    public void Master_ListsIndexesAndTriggers()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<string> indexNames = db.Pragmas.Master
            .Where(m => m.Type == "index" && m.TableName == "Books")
            .Select(m => m.Name)
            .ToList();

        Assert.Contains("IX_Book_AuthorId", indexNames);
    }

    [Fact]
    public void Master_Count_WorksAsAggregate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        int tableCount = db.Pragmas.Master.Count(m => m.Type == "table");

        Assert.True(tableCount >= 2);
    }

    [Fact]
    public void Master_PropertyIsLazy_AndCached()
    {
        using TestDatabase db = new();

        ReadOnlySQLiteTable<SQLiteMaster> first = db.Pragmas.Master;
        ReadOnlySQLiteTable<SQLiteMaster> second = db.Pragmas.Master;

        Assert.Same(first, second);
    }

    [Fact]
    public void Master_UsesSameProvider_AsRegularTable()
    {
        using TestDatabase db = new();

        Assert.Same(db, db.Pragmas.Master.Provider);
        Assert.Equal(typeof(SQLiteMaster), db.Pragmas.Master.ElementType);
    }

    [Fact]
    public void Sequence_AfterAutoIncrementInsert_TracksSequenceValue()
    {
        using TestDatabase db = new();
        db.Table<LongKeyEntity>().Schema.CreateTable();

        db.Table<LongKeyEntity>().Add(new LongKeyEntity { Name = "first" });
        db.Table<LongKeyEntity>().Add(new LongKeyEntity { Name = "second" });

        SQLiteSequence row = db.Pragmas.Sequence
            .Single(s => s.Name == "LongKeyEntity");

        Assert.Equal(2, row.LastValue);
    }

    [Fact]
    public void Sequence_BeforeAnyAutoIncrementTable_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() => db.Pragmas.Sequence.ToList());
    }

    [Fact]
    public void Master_JoinsAgainstUserTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Title = "T", AuthorId = 1, Price = 1 });

        var rows = (
            from b in db.Table<Book>()
            join m in db.Pragmas.Master on "Books" equals m.Name
            where m.Type == "table"
            select new { b.Title, TableName = m.Name }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal("Books", rows[0].TableName);
    }

    [Fact]
    public void TableInfo_DirectCall_ReturnsColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<PragmaTableInfo> columns = db.Pragmas.TableInfo("Books").ToList();

        Assert.Equal(4, columns.Count);
        Assert.Contains(columns, c => c.Name == "BookId" && c.PrimaryKeyOrder == 1);
        Assert.Contains(columns, c => c.Name == "BookTitle" && c.IsNotNull);
    }

    [Fact]
    public void TableInfo_FilteredOnNotNull_ReturnsOnlyRequiredColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<string> required = db.Pragmas.TableInfo("Books")
            .Where(c => c.IsNotNull)
            .Select(c => c.Name)
            .ToList();

        Assert.Contains("BookTitle", required);
        Assert.Contains("BookAuthorId", required);
    }

    [Fact]
    public void TableInfo_CorrelatedSelectMany_ReturnsColumnsForEveryUserTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        var rows = (
            from m in db.Pragmas.Master
            where m.Type == "table" && !m.Name.StartsWith("sqlite_")
            from p in db.Pragmas.TableInfo(m.Name)
            select new { TableName = m.Name, ColumnName = p.Name, ColumnType = p.Type }
        ).ToList();

        Assert.Contains(rows, r => r.TableName == "Books" && r.ColumnName == "BookTitle");
        Assert.Contains(rows, r => r.TableName == "Authors" && r.ColumnName == "AuthorEmail");
    }

    [Fact]
    public void IndexList_DirectCall_ReturnsIndexes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<PragmaIndexList> indexes = db.Pragmas.IndexList("Books").ToList();

        Assert.Contains(indexes, i => i.Name == "IX_Book_AuthorId");
    }

    [Fact]
    public void Master_NonGenericGetEnumerator_IteratesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int count = 0;
        foreach (object _ in (System.Collections.IEnumerable)db.Pragmas.Master)
        {
            count++;
        }

        Assert.True(count > 0);
    }

    [Fact]
    public void TableInfo_NonGenericGetEnumerator_IteratesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int count = 0;
        foreach (object _ in (System.Collections.IEnumerable)db.Pragmas.TableInfo("Books"))
        {
            count++;
        }

        Assert.Equal(4, count);
    }

    [Fact]
    public void ForeignKeyList_DirectCall_ReturnsForeignKeys()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE Parent (Id INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE Child (Id INTEGER PRIMARY KEY, ParentId INTEGER REFERENCES Parent(Id))");

        List<PragmaForeignKey> keys = db.Pragmas.ForeignKeyList("Child").ToList();

        Assert.Single(keys);
        Assert.Equal("Parent", keys[0].ReferencedTable);
        Assert.Equal("ParentId", keys[0].FromColumn);
        Assert.Equal("Id", keys[0].ToColumn);
    }
}
