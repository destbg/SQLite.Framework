using Microsoft.Data.Sqlite;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace SQLite.Framework.Tests;

public class MethodCallTests
{
    [Fact]
    public void ListContains()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        List<int> list = [1, 2, 3];

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where list.Contains(book.Id)
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal(3, command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId IN (@p0, @p1, @p2)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryableContains()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where (
                from b in db.Table<Book>()
                where b.Title == "test"
                select b.Title
            ).Contains("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("test", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE @p0 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p1
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringContains()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Contains("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringContainsComparison()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Contains("test", StringComparison.OrdinalIgnoreCase)
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p0 COLLATE NOCASE
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringStartsWith()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.StartsWith("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringEndsWith()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.EndsWith("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("%test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringEquals()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Equals("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringIndexOf()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.IndexOf("test") == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal(1, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (INSTR(b0.BookTitle, @p0)) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringReplace()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Replace("a", "b") == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal("a", command.Parameters[0].Value);
        Assert.Equal("b", command.Parameters[1].Value);
        Assert.Equal("be", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (REPLACE(b0.BookTitle, @p0, @p1)) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrim()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Trim() == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE TRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimOne()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Trim(' ') == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(' ', command.Parameters[0].Value);
        Assert.Equal("be", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (TRIM(b0.BookTitle, @p0)) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimMultiple()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Trim(' ', 'a', 'b', 'c') == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal(' ', command.Parameters[0].Value);
        Assert.Equal('a', command.Parameters[1].Value);
        Assert.Equal('b', command.Parameters[2].Value);
        Assert.Equal('c', command.Parameters[3].Value);
        Assert.Equal("be", command.Parameters[4].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (TRIM(TRIM(TRIM(TRIM(b0.BookTitle, @p0), @p1), @p2), @p3)) = @p4
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimStart()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.TrimStart() == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE LTRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimEnd()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.TrimEnd() == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE RTRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringSubstring()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Substring(1) == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("be", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (SUBSTR(b0.BookTitle, @p0)) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringSubstringRange()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Substring(1, 2) == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("be", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (SUBSTR(b0.BookTitle, @p0, @p1)) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringToUpper()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.ToUpper() == "BE"
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("BE", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE UPPER(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringToLower()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where book.Title.ToLower() == "be"
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE LOWER(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathMin()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Min(book.Id, book.AuthorId) == 1
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
                     WHERE (CASE WHEN b0.BookId > b0.BookAuthorId THEN b0.BookAuthorId ELSE b0.BookId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathMax()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Max(book.Id, book.AuthorId) == 1
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
                     WHERE (CASE WHEN b0.BookId < b0.BookAuthorId THEN b0.BookAuthorId ELSE b0.BookId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathAbs()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Abs(book.Id) == 1
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
                     WHERE ABS(b0.BookId) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathRound()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Round((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ROUND(b0.BookId) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathCeiling()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Ceiling((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE CEIL(b0.BookId) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathFloor()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            where Math.Floor((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE FLOOR(b0.BookId) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}