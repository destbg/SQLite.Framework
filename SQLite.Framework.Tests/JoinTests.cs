using Microsoft.Data.Sqlite;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.DTObjects;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class JoinTests
{
    [Fact]
    public void RightJoin()
    {
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select author
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
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
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            select author
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
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
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            join author2 in db.Table<Author>() on book.AuthorId equals author2.Id
            select author
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
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
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            join author2 in db.Table<Author>() on book.AuthorId equals author2.Id into author2Group
            from author2 in author2Group.DefaultIfEmpty()
            select author
        ).ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
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
        using SQLiteDatabase db = new("Data Source=:memory:");

        using SqliteCommand command = db.Table<Book>()
            .Join(db.Table<Author>(), f => f.AuthorId, f => f.Id, (f, s) => new Testing { Book = f, Author = s })
            .Select(f => f.Author)
            .ToSqlCommand();

        Assert.Equal(0, command.Parameters.Count);
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