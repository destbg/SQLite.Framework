using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class OtherTests
{
    [Fact]
    public void QueryableContainsWithPassingArgument()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (
                from b in db.Table<Book>()
                where b.Title == "test" && book.AuthorId == b.AuthorId
                select b.Title
            ).Contains("test 2")
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("test 2", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE @p1 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE (b1.BookTitle = @p0) AND (b0.BookAuthorId = b1.BookAuthorId)
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderBys()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            orderby book.Title, book.Id descending
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     ORDER BY b0.BookTitle, b0.BookId DESC
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TakeSkip()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Take(1).Skip(2).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     LIMIT 1
                     OFFSET 2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Union()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Where(f => f.Id == 1).Union(db.Table<Book>()).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     UNION
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     WHERE b1.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Concat()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Where(f => f.Id == 1).Concat(db.Table<Book>()).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     UNION ALL
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     WHERE b1.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CheckTableMappingCached()
    {
        using TestDatabase db = new();

        TableMapping firstTableMapping = db.TableMapping<Book>();
        TableMapping secondTableMapping = db.TableMapping<Book>();

        Assert.Same(firstTableMapping, secondTableMapping);
    }

    [Fact]
    public void CheckEnum()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);

        Assert.NotNull(publisher);
        Assert.Equal(1, publisher.Id);
        Assert.Equal("test", publisher.Name);
        Assert.Equal(PublisherType.Magazine, publisher.Type);
    }
}