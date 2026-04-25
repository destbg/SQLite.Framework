using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinTests
{
    [Fact]
    public void CrossJoin()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            from author in db.Table<Author>()
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     CROSS JOIN "Authors" AS a1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void RightJoin()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LeftJoin()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     LEFT JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TwoJoins()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            join author2 in db.Table<Author>() on book.AuthorId equals author2.Id
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     JOIN "Authors" AS a2 ON b0.BookAuthorId = a2.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TwoJoinsWithLeft()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            join author2 in db.Table<Author>() on book.AuthorId equals author2.Id into author2Group
            from author2 in author2Group.DefaultIfEmpty()
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     LEFT JOIN "Authors" AS a2 ON b0.BookAuthorId = a2.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FluentRightJoin()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Join(db.Table<Author>(), f => f.AuthorId, f => f.Id, (f, s) => new Testing { Book = f, Author = s })
            .Select(f => f.Author)
            .ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void JoinWithInnerQuery()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in from a in db.Table<Author>()
                           where a.Name == "John Doe"
                           select a on book.AuthorId equals author.Id
            select author
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("John Doe", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT a2.Id AS "Id",
                            a2.Name AS "Name",
                            a2.Email AS "Email",
                            a2.BirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN (
                         SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                         FROM "Authors" AS a1
                         WHERE a1.AuthorName = @p0
                     ) AS a2 ON b0.BookAuthorId = a2.Id
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexJoinQuery()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on new { Id = book.AuthorId, book.Price } equals new { author.Id, Price = 0d }
            select author
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(0d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON a1.AuthorId = b0.BookAuthorId AND @p0 = b0.BookPrice
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexJoinWithPropertyComparison()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on new { Id = book.AuthorId, Active = true }
                equals new { author.Id, Active = author.Email != null }
            select author
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(true, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON a1.AuthorId = b0.BookAuthorId AND a1.AuthorEmail IS NOT NULL = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexJoinWithMultiplePropertyComparisons()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on new { Id = book.AuthorId, HasEmail = book.Title != null }
                equals new { author.Id, HasEmail = author.Email != null }
            select author
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a1.AuthorId AS "Id",
                            a1.AuthorName AS "Name",
                            a1.AuthorEmail AS "Email",
                            a1.AuthorBirthDate AS "BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON a1.AuthorId = b0.BookAuthorId AND a1.AuthorEmail IS NOT NULL = b0.BookTitle IS NOT NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LeftJoinWithNullCheck()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            select new { Book = book.Title, HasAuthor = author != null }
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookTitle AS "Book",
                            a1.AuthorId IS NOT NULL AS "HasAuthor"
                     FROM "Books" AS b0
                     LEFT JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LeftJoinWithNullCheckInWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            where author != null
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     LEFT JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     WHERE a1.AuthorId IS NOT NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LeftJoinWithNotNullCheckInWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            where author == null
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     LEFT JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     WHERE a1.AuthorId IS NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    private class Testing
    {
        public required Book Book { get; set; }
        public required Author Author { get; set; }
    }
}