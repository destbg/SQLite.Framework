using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteTableSchemaDelegateTests
{
    [Fact]
    public void DropTable_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Schema.DropTable();

        Assert.False(db.Schema.TableExists<Book>());
    }

    [Fact]
    public void TableExists_Delegates()
    {
        using TestDatabase db = new();
        Assert.False(db.Table<Book>().Schema.TableExists());

        db.Table<Book>().Schema.CreateTable();

        Assert.True(db.Table<Book>().Schema.TableExists());
    }

    [Fact]
    public void RenameTable_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Schema.RenameTable("Books_Renamed");

        Assert.True(db.Schema.TableExists("Books_Renamed"));
    }

    [Fact]
    public void ColumnExists_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.True(db.Table<Book>().Schema.ColumnExists("BookTitle"));
        Assert.False(db.Table<Book>().Schema.ColumnExists("NoSuchColumn"));
    }

    [Fact]
    public void ListColumns_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Contains(db.Table<Book>().Schema.ListColumns(), c => c.Name == "BookId");
    }

    [Fact]
    public void CreateIndex_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Schema.CreateIndex(b => b.Title, name: "IX_Deleg_Title", unique: true);

        Assert.True(db.Schema.IndexExists("IX_Deleg_Title"));
    }

    [Fact]
    public void AddColumn_PropertyName_Delegates()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Table<EvolvingTablePlusName>().Schema.AddColumn(nameof(EvolvingTablePlusName.Name));

        Assert.True(db.Table<EvolvingTablePlusName>().Schema.ColumnExists("Name"));
    }

    [Fact]
    public void AddColumn_Selector_Delegates()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();

        db.Table<EvolvingTablePlusName>().Schema.AddColumn(e => e.Name);

        Assert.True(db.Table<EvolvingTablePlusName>().Schema.ColumnExists("Name"));
    }

    [Fact]
    public void AddColumn_PropertyName_ExpressionDefault_Delegates()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO \"Evolving\" (\"Id\") VALUES (1)");

        db.Table<EvolvingTablePlusName>().Schema.AddColumn(nameof(EvolvingTablePlusName.Name), () => "from-db");

        Assert.Equal("from-db", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"Evolving\""));
    }

    [Fact]
    public void AddColumn_Selector_ExpressionDefault_Delegates()
    {
        using TestDatabase db = new();
        db.Table<EvolvingTable>().Schema.CreateTable();
        db.Execute("INSERT INTO \"Evolving\" (\"Id\") VALUES (1)");

        db.Table<EvolvingTablePlusName>().Schema.AddColumn(e => e.Name, () => "from-db");

        Assert.Equal("from-db", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"Evolving\""));
    }

    [Fact]
    public void RenameColumn_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Schema.RenameColumn("BookTitle", "RenamedTitle");

        Assert.True(db.Table<Book>().Schema.ColumnExists("RenamedTitle"));
    }

    [Fact]
    public void DropColumn_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Schema.DropColumn("BookTitle");

        Assert.False(db.Table<Book>().Schema.ColumnExists("BookTitle"));
    }

    [Fact]
    public void CreateView_ViewExists_DropView_Delegate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<BookView>().Schema.CreateView(() =>
            from b in db.Table<Book>()
            select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        Assert.True(db.Table<BookView>().Schema.ViewExists());

        db.Table<BookView>().Schema.DropView();

        Assert.False(db.Table<BookView>().Schema.ViewExists());
    }

    [Fact]
    public void CreateTrigger_StringBody_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Table<Book>().Schema.CreateTrigger(
            "trg_deleg_string", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
            "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.BookId, 0, NEW.BookPrice)");

        Assert.NotNull(db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'trg_deleg_string'"));
    }

    [Fact]
    public void CreateTrigger_Linq_Delegates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Table<Book>().Schema.CreateTrigger(
            "trg_deleg_linq", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
                .Insert(db.Table<BookHistory>(), s => s.Set(h => h.BookId, _ => t.New.Id)));

        Assert.NotNull(db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'trg_deleg_linq'"));
    }
}
