using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinTests
{
    // TODO: Complex join on new { fields } equals new { fields }

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
            join author in (
                from a in db.Table<Author>()
                where a.Name == "John Doe"
                select a
            ) on book.AuthorId equals author.Id
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

    class Testing
    {
        public required Book Book { get; set; }
        public required Author Author { get; set; }
    }
}