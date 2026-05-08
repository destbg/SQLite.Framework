using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
namespace SQLite.Framework.Tests;

public class AsyncSQLiteSourceUnsupportedTests
{
    private static TestDatabase Db()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public async Task FirstOrDefaultAsync_PredicateAndDefaultValue_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book result = await db.Table<Book>().FirstOrDefaultAsync(b => b.Id == 1, def);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_PredicateAndDefaultValue_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book result = await db.Table<Book>().SingleOrDefaultAsync(b => b.Id == 1, def);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task ToDictionaryAsync_KeyAndValueSelector_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        Dictionary<int, string> map = await db.Table<Book>().ToDictionaryAsync(b => b.Id, b => b.Title);
        Assert.Equal("A", map[1]);
    }

    [Fact]
    public async Task SingleAsync_NoPredicate_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        Book row = await db.Table<Book>().SingleAsync();
        Assert.Equal(1, row.Id);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_NoPredicate_OnSqlite()
    {
        using TestDatabase db = Db();
        Book? row = await db.Table<Book>().SingleOrDefaultAsync();
        Assert.Null(row);
    }

    [Fact]
    public async Task LongCountAsync_NoPredicate_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        long c = await db.Table<Book>().LongCountAsync();
        Assert.Equal(1L, c);
    }

    [Fact]
    public async Task LongCountAsync_Predicate_OnSqlite()
    {
        using TestDatabase db = Db();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        long c = await db.Table<Book>().LongCountAsync(b => b.Id == 1);
        Assert.Equal(1L, c);
    }
}
#endif
