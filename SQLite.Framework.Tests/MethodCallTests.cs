using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace SQLite.Framework.Tests;

public class MethodCallTests
{
    [Fact]
    public void ListMax()
    {
        using TestDatabase db = new();

        List<int> list = [1, 2, 3];

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == list.Max()
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(3, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ListContains()
    {
        using TestDatabase db = new();

        List<int> list = [1, 2, 3];

        SQLiteCommand command = (
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
                     WHERE b0.BookId IN (@p1, @p2, @p3)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryableContains()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE @p1 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p0
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryableMax()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == (
                from b in db.Table<Book>()
                where b.Title == "test"
                select b.Id
            ).Max()
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = (
                         SELECT MAX(b1.BookId) AS "7"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p0
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringContains()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Contains("test")
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringContainsComparison()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Contains("test", StringComparison.OrdinalIgnoreCase)
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p2 ESCAPE '\' COLLATE NOCASE
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringStartsWith()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.StartsWith("test")
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringEndsWith()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.EndsWith("test%")
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test\\%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringEquals()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Equals("test")
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE INSTR(b0.BookTitle, @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringReplace()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE REPLACE(b0.BookTitle, @p0, @p1) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrim()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Trim() == "be"
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE TRIM(b0.BookTitle, @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimMultiple()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE TRIM(TRIM(TRIM(TRIM(b0.BookTitle, @p4), @p5), @p6), @p7) = @p8
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringTrimStart()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.TrimStart() == "be"
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.TrimEnd() == "be"
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE SUBSTR(b0.BookTitle, @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringSubstringRange()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
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
                     WHERE SUBSTR(b0.BookTitle, @p0, @p1) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringToUpper()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.ToUpper() == "BE"
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.ToLower() == "be"
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Min(book.Id, book.AuthorId) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN b0.BookId < b0.BookAuthorId THEN b0.BookId ELSE b0.BookAuthorId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathMax()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Max(book.Id, book.AuthorId) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN b0.BookId > b0.BookAuthorId THEN b0.BookId ELSE b0.BookAuthorId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathAbs()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Abs(book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
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
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ROUND(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathCeiling()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Ceiling((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE CEIL(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathFloor()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Floor((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE FLOOR(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}