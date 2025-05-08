using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinTests
{
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

    class Testing
    {
        public required Book Book { get; set; }
        public required Author Author { get; set; }
    }
}