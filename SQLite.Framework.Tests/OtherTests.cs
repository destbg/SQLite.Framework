using Microsoft.Data.Sqlite;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class OtherTests
{
    [Fact]
    public void QueryableContainsWithPassingArgument()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where (
                from b in db.Table<Book>()
                where b.Title == "test" && book.AuthorId == b.AuthorId
                select b.Title
            ).Contains("test 2")
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("test 2", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE @p1 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE (b1.BookTitle = @p0) AND (b0.BookAuthorId = b1.BookAuthorId)
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderBys()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            orderby book.Title, book.Id descending
            select book
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     ORDER BY b0.BookTitle, b0.BookId DESC
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TakeSkip()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = db.Table<Book>().Take(1).Skip(2).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     LIMIT 1
                     OFFSET 2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Union()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = db.Table<Book>().Where(f => f.Id == 1).Union(db.Table<Book>()).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     UNION
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     WHERE b1.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Concat()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = db.Table<Book>().Where(f => f.Id == 1).Concat(db.Table<Book>()).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     UNION ALL
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     WHERE b1.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CheckTableMappingCached()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        TableMapping firstTableMapping = db.TableMapping<Book>();
        TableMapping secondTableMapping = db.TableMapping<Book>();

        Assert.Same(firstTableMapping, secondTableMapping);
    }
}