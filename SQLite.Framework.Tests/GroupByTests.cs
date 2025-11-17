using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByTests
{
    [Fact]
    public void GroupByTableWithMultiResult()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            where g.Count() > 1
            select new { Count = g.Count(), Id = g.Key }
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT COUNT(*) AS "Count",
                            b0.BookAuthorId AS "Id"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     HAVING COUNT(*) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupBySumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupBySimpleSumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book.Price by book.AuthorId
            into g
            select g.Sum()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByComplexSumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group new { book.Price, book.AuthorId } by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByAverageTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Average(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT AVG(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByMinTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Min(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT MIN(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByMaxTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Max(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT MAX(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByCountTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Count()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT COUNT(*) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByLongCountTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.LongCount()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT COUNT(*) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByCountHavingTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            where g.Count() > 1
            select g.Count()
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT COUNT(*) AS "8"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     HAVING COUNT(*) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}