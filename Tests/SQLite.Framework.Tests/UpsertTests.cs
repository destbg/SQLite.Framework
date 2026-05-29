using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpsertTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void Upsert_DoNothing_EmitsExpectedSqlAndKeepsExistingRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSql(c => c.OnConflict(b => b.Id).DoNothing());
        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookId) DO NOTHING"),
            N(sql));

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });
        int affected = db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "ignored", AuthorId = 2, Price = 9 },
            c => c.OnConflict(b => b.Id).DoNothing());

        Assert.Equal(0, affected);
        Book stored = db.Table<Book>().Single();
        Assert.Equal("original", stored.Title);
        Assert.Equal(5, stored.Price);
    }

    [Fact]
    public void Upsert_DoUpdateAll_EmitsExpectedSqlAndUpdatesAllNonKeyColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSql(c => c.OnConflict(b => b.Id).DoUpdateAll());
        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookId) DO UPDATE SET BookTitle = excluded.BookTitle, BookAuthorId = excluded.BookAuthorId, BookPrice = excluded.BookPrice"),
            N(sql));

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });
        db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "updated", AuthorId = 9, Price = 7 },
            c => c.OnConflict(b => b.Id).DoUpdateAll());

        Book stored = db.Table<Book>().Single();
        Assert.Equal("updated", stored.Title);
        Assert.Equal(9, stored.AuthorId);
        Assert.Equal(7, stored.Price);
    }

    [Fact]
    public void Upsert_DoUpdateSpecific_EmitsExpectedSqlAndUpdatesOnlyListedColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSql(c => c.OnConflict(b => b.Id).DoUpdate(b => b.Title, b => b.Price));
        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookId) DO UPDATE SET BookTitle = excluded.BookTitle, BookPrice = excluded.BookPrice"),
            N(sql));

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });
        db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "updated", AuthorId = 9, Price = 7 },
            c => c.OnConflict(b => b.Id).DoUpdate(b => b.Title, b => b.Price));

        Book stored = db.Table<Book>().Single();
        Assert.Equal("updated", stored.Title);
        Assert.Equal(7, stored.Price);
        Assert.Equal(1, stored.AuthorId);
    }

    [Fact]
    public void Upsert_OnConflictComposite_EmitsExpectedSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSql(c => c.OnConflict(b => new { b.AuthorId, b.Title }).DoUpdate(b => b.Price));
        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookAuthorId, BookTitle) DO UPDATE SET BookPrice = excluded.BookPrice"),
            N(sql));
    }

    [Fact]
    public void UpsertRange_RunsHooksPerRow()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnAddOrUpdate<Book>(_ => hookCount++));
        db.Table<Book>().Schema.CreateTable();

        int affected = db.Table<Book>().UpsertRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 },
        }, c => c.OnConflict(b => b.Id).DoUpdateAll());

        Assert.Equal(3, affected);
        Assert.Equal(3, hookCount);
        Assert.Equal(3, db.Table<Book>().Count());
    }

    [Fact]
    public void Upsert_RunsAddOrUpdateHook()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnAddOrUpdate<Book>(_ => hookCount++));
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            c => c.OnConflict(b => b.Id).DoNothing());

        Assert.Equal(1, hookCount);
    }

    [Fact]
    public void Upsert_HookReturnsFalse_SkipsInsertReturnsZero()
    {
        using TestDatabase db = new(b => b.OnAddOrUpdate<Book>((_, _) => false));
        db.Table<Book>().Schema.CreateTable();

        int affected = db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            c => c.OnConflict(b => b.Id).DoUpdateAll());

        Assert.Equal(0, affected);
        Assert.Empty(db.Table<Book>().ToList());
    }

    [Fact]
    public void Upsert_OnConflictMissingMethod_ThrowsAtConfigure()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Upsert(
                new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
                _ => { }));
    }

    [Fact]
    public void Upsert_DoUpdateWithNoColumns_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().Upsert(
                new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
                c => c.OnConflict(b => b.Id).DoUpdate()));
    }

    [Fact]
    public void Upsert_OnConflictCalledTwice_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Upsert(
                new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
                c =>
                {
                    c.OnConflict(b => b.Id);
                    c.OnConflict(b => b.AuthorId);
                }));

        Assert.Equal("OnConflict was already called for this Upsert.", ex.Message);
    }

    [Fact]
    public void Upsert_OnConflict_NewExpressionWithNonMember_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        Assert.Throws<NotSupportedException>(() =>
            inspector.GetSql(c => c.OnConflict(b => new { b.Id, Lit = 5 }).DoNothing()));
    }

    [Fact]
    public void Upsert_OnConflict_NonMemberBody_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        Assert.Throws<NotSupportedException>(() =>
            inspector.GetSql(c => c.OnConflict(b => 5).DoNothing()));
    }

    [Fact]
    public void Upsert_DoUpdate_NonMemberArg_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        Assert.Throws<NotSupportedException>(() =>
            inspector.GetSql(c => c.OnConflict(b => b.Id).DoUpdate(b => b.Price + 1)));
    }

    [Fact]
    public void Upsert_OnConflictWhere_EmitsPartialIndexTarget()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSql(c => c.OnConflict(b => b.Title).Where(b => b.AuthorId == 1).DoUpdate(b => b.Price));
        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookTitle) WHERE BookAuthorId = 1 DO UPDATE SET BookPrice = excluded.BookPrice"),
            N(sql));
    }

    [Fact]
    public void Upsert_OnConflictWhere_UpdatesConflictingRowInPartialIndex()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Execute("CREATE UNIQUE INDEX ux_books_title_author1 ON \"Books\" (BookTitle) WHERE BookAuthorId = 1");

        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        int affected = db.Table<Book>().Upsert(
            new Book { Id = 2, Title = "x", AuthorId = 1, Price = 99 },
            c => c.OnConflict(b => b.Title).Where(b => b.AuthorId == 1).DoUpdate(b => b.Price));

        List<Book> rows = db.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Equal(1, affected);
        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(99, rows[0].Price);
    }

    [Fact]
    public void Upsert_OnConflictWhere_InsertsWhenRowOutsidePartialIndex()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Execute("CREATE UNIQUE INDEX ux_books_title_author1 ON \"Books\" (BookTitle) WHERE BookAuthorId = 1");

        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        db.Table<Book>().Upsert(
            new Book { Id = 2, Title = "x", AuthorId = 2, Price = 99 },
            c => c.OnConflict(b => b.Title).Where(b => b.AuthorId == 1).DoUpdate(b => b.Price));

        List<Book> rows = db.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(5, rows[0].Price);
        Assert.Equal(99, rows[1].Price);
    }

    [Fact]
    public void Upsert_WhereCalledTwice_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Upsert(
                new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
                c => c.OnConflict(b => b.Title).Where(b => b.AuthorId == 1).Where(b => b.Price > 0).DoNothing()));

        Assert.Equal("Where was already called for this OnConflict target.", ex.Message);
    }

    private sealed class SqlInspectingTable : SQLiteTable<Book>
    {
        public SqlInspectingTable(SQLiteDatabase database, TableMapping table) : base(database, table) { }

        public string GetSql(Action<UpsertBuilder<Book>> configure) => GetUpsertInfo(configure).Sql;
    }
}
