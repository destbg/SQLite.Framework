using Microsoft.Data.Sqlite;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class WhereTests
{
    [Fact]
    public void EqualWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NotEqualWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id != 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId <> @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GreaterThanWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id > 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GreaterThanOrEqualWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id >= 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId >= @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LessThanWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id < 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId < @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LessThanOrEqualWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id <= 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId <= @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void AddWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id + 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId + @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void SubtractWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id - 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId - @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MultiplyWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id * 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId * @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DivideWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id / 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId / @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void AndWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1 && book.AuthorId == 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId = @p0) AND (b0.BookAuthorId = @p1)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1 || book.AuthorId == 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId = @p0) OR (b0.BookAuthorId = @p1)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void IsWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title == null
            select book
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle IS NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void IsNotWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title != null
            select book
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle IS NOT NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ConditionalWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where (book.Title != null ? 1 : 2) == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal(1, command.Parameters[2].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM "Books" AS b0
                      WHERE (CASE WHEN (b0.BookTitle IS NOT NULL) THEN @p0 ELSE @p1 END) = @p2
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CoalesceWhere()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where (book.Title ?? "") == "Book"
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("", command.Parameters[0].Value);
        Assert.Equal("Book", command.Parameters[1].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM "Books" AS b0
                      WHERE (COALESCE(b0.BookTitle, @p0)) = @p1
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}