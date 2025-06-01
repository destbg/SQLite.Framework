using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.DTObjects;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SelectTests
{
    [Fact]
    public void SelectTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DirectSelect()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void SelectToDTO()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from author in db.Table<Author>()
            select new AuthorDTO
            {
                Id = author.Id,
                Email = author.Email,
                Name = author.Name,
                BirthDate = author.BirthDate
            }
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT a0.AuthorId AS "Id",
                            a0.AuthorEmail AS "Email",
                            a0.AuthorName AS "Name",
                            a0.AuthorBirthDate AS "BirthDate"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DistinctSelectToDTO()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from author in db.Table<Author>()
            select new AuthorDTO
            {
                Id = author.Id,
                Email = author.Email,
                Name = author.Name,
                BirthDate = author.BirthDate
            }
        ).Distinct().ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT DISTINCT a0.AuthorId AS "Id",
                            a0.AuthorEmail AS "Email",
                            a0.AuthorName AS "Name",
                            a0.AuthorBirthDate AS "BirthDate"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DeepSelectToDTO()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new BookDTO
            {
                Id = book.Id,
                Title = book.Title,
                Author = new AuthorDTO
                {
                    Id = author.Id,
                    Email = author.Email,
                    Name = author.Name,
                    BirthDate = author.BirthDate
                }
            }
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            a1.AuthorId AS "Author.Id",
                            a1.AuthorEmail AS "Author.Email",
                            a1.AuthorName AS "Author.Name",
                            a1.AuthorBirthDate AS "Author.BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexSelectToDTO()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from author in db.Table<Author>()
            select new AuthorDTO
            {
                Id = author.Id + 1,
                Email = author.Email + ".net",
                Name = author.Name + " Mike",
                BirthDate = author.BirthDate.AddDays(5)
            }
        ).ToSqlCommand();

        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(".net", command.Parameters[1].Value);
        Assert.Equal(" Mike", command.Parameters[2].Value);
        Assert.Equal(5d, command.Parameters[3].Value);
        Assert.Equal(864000000000, command.Parameters[4].Value);
        Assert.Equal("""
                     SELECT a0.AuthorId + @p0 AS "Id",
                            a0.AuthorEmail + @p1 AS "Email",
                            a0.AuthorName + @p2 AS "Name",
                            CAST(a0.AuthorBirthDate + (@p3 * @p4) AS 'INTEGER') AS "BirthDate"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MultipleSelects()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from author in db.Table<Author>()
            select new AuthorDTO
            {
                Id = author.Id + 1,
                Email = author.Email + ".net",
                Name = author.Name + " Mike",
                BirthDate = author.BirthDate.AddDays(5)
            }
        ).Select(f => f.Id - 1).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(1, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT a0.AuthorId + @p0 - @p5 AS "8"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }
}