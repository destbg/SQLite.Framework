using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathOperationTests
{
    [Fact]
    public void MathAbs()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Abs(book.Price - 10) < 1
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal(1d, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ABS((b0.BookPrice - @p0)) < @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathRound()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round(book.Price) == 10
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ROUND(b0.BookPrice) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathRoundWithDigits()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round(book.Price, 2) == 10.50
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal(10.50d, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ROUND(b0.BookPrice, @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathPow()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Pow(book.Id, 2) == 4
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2d, command.Parameters[0].Value);
        Assert.Equal(4d, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE POWER(CAST(b0.BookId AS REAL), @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathFloor()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Floor(book.Price) == 10
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE FLOOR(b0.BookPrice) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathCeiling()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Ceiling(book.Price) == 11
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(11d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE CEIL(b0.BookPrice) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathMaxWithResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 15 }
        });

        var result = db.Table<Book>()
            .Select(b => new { b.Id, MaxPrice = Math.Max(b.Price, 8) })
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(8, result[0].MaxPrice);
        Assert.Equal(10, result[1].MaxPrice);
        Assert.Equal(15, result[2].MaxPrice);
    }

    [Fact]
    public void MathMinWithResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 15 }
        });

        var result = db.Table<Book>()
            .Select(b => new { b.Id, MinPrice = Math.Min(b.Price, 8) })
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(5, result[0].MinPrice);
        Assert.Equal(8, result[1].MinPrice);
        Assert.Equal(8, result[2].MinPrice);
    }

    [Fact]
    public void ArithmeticAddition()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price + 5
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT (b0.BookPrice + @p0) AS "4"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticSubtraction()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price - 5
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT (b0.BookPrice - @p0) AS "4"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticMultiplication()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price * 1.1
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1.1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT (b0.BookPrice * @p0) AS "4"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticDivision()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price / 2
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(2d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT (b0.BookPrice / @p0) AS "4"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticModulo()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id % 2 == 0
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal(0, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId % @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexMathExpression()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Price * 1.1 + 5 > 20
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1.1d, command.Parameters[0].Value);
        Assert.Equal(5d, command.Parameters[1].Value);
        Assert.Equal(20d, command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ((b0.BookPrice * @p0) + @p1) > @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}