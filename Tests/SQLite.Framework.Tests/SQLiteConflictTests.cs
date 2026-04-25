using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteConflictTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void AddOrUpdate_DefaultReplace_OverwritesRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "first", AuthorId = 1, Price = 5 });
        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "second", AuthorId = 1, Price = 7 });

        Book stored = db.Table<Book>().Single();
        Assert.Equal("second", stored.Title);
        Assert.Equal(7, stored.Price);
    }

    [Fact]
    public void AddOrUpdate_Ignore_KeepsExistingRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        int affected = db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "ignored", AuthorId = 1, Price = 9 }, SQLiteConflict.Ignore);

        Assert.Equal(0, affected);
        Book stored = db.Table<Book>().Single();
        Assert.Equal("original", stored.Title);
        Assert.Equal(5, stored.Price);
    }

    [Fact]
    public void AddOrUpdate_Abort_ThrowsOnConflict()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        Assert.Throws<SQLiteException>(() =>
            db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "blocked", AuthorId = 1, Price = 9 }, SQLiteConflict.Abort));

        Book stored = db.Table<Book>().Single();
        Assert.Equal("original", stored.Title);
    }

    [Fact]
    public void AddOrUpdate_Fail_ThrowsOnConflict()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        Assert.Throws<SQLiteException>(() =>
            db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "blocked", AuthorId = 1, Price = 9 }, SQLiteConflict.Fail));

        Book stored = db.Table<Book>().Single();
        Assert.Equal("original", stored.Title);
    }

    [Fact]
    public void AddOrUpdate_Rollback_ThrowsAndRollsBackTransaction()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        Assert.Throws<SQLiteException>(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            db.Table<Book>().Add(new Book { Id = 2, Title = "new", AuthorId = 1, Price = 7 });
            db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "blocked", AuthorId = 1, Price = 9 }, SQLiteConflict.Rollback);
            tx.Commit();
        });

        List<Book> all = db.Table<Book>().ToList();
        Assert.Single(all);
        Assert.Equal(1, all[0].Id);
        Assert.Equal("original", all[0].Title);
    }

    [Fact]
    public void AddOrUpdateRange_Ignore_SkipsConflictRowsInsertsRest()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        int affected = db.Table<Book>().AddOrUpdateRange(new[]
        {
            new Book { Id = 1, Title = "skipped", AuthorId = 1, Price = 9 },
            new Book { Id = 2, Title = "fresh", AuthorId = 1, Price = 7 },
        }, conflict: SQLiteConflict.Ignore);

        Assert.Equal(1, affected);
        List<Book> all = db.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Equal(2, all.Count);
        Assert.Equal("original", all[0].Title);
        Assert.Equal("fresh", all[1].Title);
    }

    [Theory]
    [InlineData(SQLiteConflict.Replace, "OR REPLACE")]
    [InlineData(SQLiteConflict.Ignore, "OR IGNORE")]
    [InlineData(SQLiteConflict.Abort, "OR ABORT")]
    [InlineData(SQLiteConflict.Fail, "OR FAIL")]
    [InlineData(SQLiteConflict.Rollback, "OR ROLLBACK")]
    public void AddOrUpdate_EachConflictMode_EmitsExpectedSql(SQLiteConflict mode, string clause)
    {
        using TestDatabase db = new();
        SqlInspectingTable inspector = new(db, db.TableMapping(typeof(Book)));

        string sql = inspector.GetSqlForConflict(mode);

        Assert.Equal(N($"INSERT {clause} INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3)"), N(sql));
    }

    private sealed class SqlInspectingTable : SQLiteTable<Book>
    {
        public SqlInspectingTable(SQLiteDatabase database, SQLite.Framework.Models.TableMapping table) : base(database, table) { }

        public string GetSqlForConflict(SQLiteConflict conflict) => GetAddOrUpdateInfo(conflict).Sql;
    }
}
