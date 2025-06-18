using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class OtherTests
{
    private class BaseCastEntity
    {
        [Key, AutoIncrement]
        public int Id { get; set; }
    }

    private class CastEntity : BaseCastEntity
    {
        public required string Text { get; set; }
    }

    private class RequiredEntity
    {
        [Key, AutoIncrement]
        public int Id { get; set; }

        [Required]
        public string? Date { get; set; }
    }

    [Fact]
    public void TestUniqueness()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "test",
            AuthorId = 1,
            Price = 10
        });

        Assert.Throws<SQLiteException>(() =>
        {
            db.Table<Book>().Add(new Book
            {
                Id = 2,
                Title = "test",
                AuthorId = 1,
                Price = 10
            });
        });
    }

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
                         WHERE b1.BookTitle = @p0 AND b0.BookAuthorId = b1.BookAuthorId
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
                     ORDER BY b0.BookTitle ASC, b0.BookId DESC
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
    public void CheckTableMappingCachedNonGeneric()
    {
        using TestDatabase db = new();

        TableMapping firstTableMapping = db.TableMapping(typeof(Book));
        TableMapping secondTableMapping = db.TableMapping(typeof(Book));

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

    [Fact]
    public void CheckParameterToString()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(f => f.Id == 1)
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("@p0 = 1", command.Parameters[0].ToString());
    }

    [Fact]
    public void RollbackTransaction()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        using SQLiteTransaction transaction = db.BeginTransaction();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        transaction.Rollback();

        // After rollback, the data should not be present in the table
        Assert.Throws<InvalidOperationException>(() =>
        {
            Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);
        });
    }

    [Fact]
    public void AutoRollbackTransaction()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        {
            using SQLiteTransaction transaction = db.BeginTransaction();

            db.Table<Publisher>().Add(new Publisher
            {
                Id = 1,
                Name = "test",
                Type = PublisherType.Magazine
            });
        }

        // After rollback, the data should not be present in the table
        Assert.Throws<InvalidOperationException>(() =>
        {
            Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);
        });
    }

    [Fact]
    public void EnumerateTable()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        foreach (Publisher publisher in db.Table<Publisher>())
        {
            Assert.Equal(1, publisher.Id);
            Assert.Equal("test", publisher.Name);
            Assert.Equal(PublisherType.Magazine, publisher.Type);
        }
    }

    [Fact]
    public void RequiredAttributeInTable()
    {
        using TestDatabase db = new();

        db.Table<RequiredEntity>().CreateTable();

        db.Table<RequiredEntity>().Add(new RequiredEntity
        {
            Date = "2000"
        });

        RequiredEntity publisher = db.Table<RequiredEntity>().First();

        Assert.Equal(1, publisher.Id);
        Assert.Equal("2000", publisher.Date);
    }

    [Fact]
    public void CheckTableMappingExists()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        Assert.Single(db.TableMappings);
    }

    [Fact]
    public void GetNonGenericTable()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        SQLiteTable table = db.Table(typeof(Publisher));

        foreach (Publisher publisher in table)
        {
            Assert.Equal(1, publisher.Id);
            Assert.Equal("test", publisher.Name);
            Assert.Equal(PublisherType.Magazine, publisher.Type);
        }
    }

    [Fact]
    public void CastTable()
    {
        using TestDatabase db = new();

        db.Table<CastEntity>().CreateTable();

        db.Table<CastEntity>().Add(new CastEntity
        {
            Text = "test"
        });

        List<BaseCastEntity> table = db.Table(typeof(CastEntity))
            .Cast<BaseCastEntity>()
            .ToList();

        Assert.Single(table);
        Assert.Equal(1, table[0].Id);
        Assert.IsNotType<CastEntity>(table[0]);
    }

    [Fact]
    public void QueryTableByOnlySQL()
    {
        using TestDatabase db = new();

        db.OpenConnection();

        SQLiteCommand command = new(db)
        {
            CommandText = """
                          CREATE TABLE IF NOT EXISTS "TestTable" (
                              "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
                              "Name" TEXT NOT NULL
                          )
                          """,
            Parameters = new List<SQLiteParameter>()
        };

        command.ExecuteNonQuery();

        SQLiteCommand insertCommand = new(db)
        {
            CommandText = "INSERT INTO \"TestTable\" (\"Name\") VALUES (@name)",
            Parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter
                {
                    Name = "@name",
                    Value = "Test Name"
                }
            }
        };

        insertCommand.ExecuteNonQuery();

        SQLiteCommand queryCommand = new(db)
        {
            CommandText = "SELECT * FROM \"TestTable\"",
            Parameters = new List<SQLiteParameter>()
        };

        using SQLiteDataReader reader = queryCommand.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetValue(0, SQLiteColumnType.Integer, typeof(int))); // Id
        Assert.Equal("Test Name", reader.GetValue(1, SQLiteColumnType.Text, typeof(string))); // Name
    }

    [Fact]
    public void CheckCallingOpenConnectionTwice()
    {
        using TestDatabase db = new();

        db.OpenConnection();
        db.OpenConnection();

        Assert.True(db.IsConnected);
    }

    [Fact]
    public async Task CheckCallingOpenConnectionFromDifferentThreads()
    {
        using TestDatabase db = new();

        Task task1 = Task.Run(() => db.OpenConnection());
        Task task2 = Task.Run(() => db.OpenConnection());
        await Task.WhenAll(task1, task2);

        Assert.True(db.IsConnected);
    }

    [Fact]
    public void FromSqlCompilesToSqlAndReturnsResult()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "FromSqlTest",
            AuthorId = 2,
            Price = 99
        });

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";
        IQueryable<Book> table = db.Table<Book>()
            .FromSql(sql, new SQLiteParameter
            {
                Name = "@title",
                Value = "FromSqlTest"
            })
            .Where(f => f.Id == 1);

        SQLiteCommand command = table.ToSqlCommand();
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      WHERE b0.BookId = @p1
                      """, command.CommandText);
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("@title", command.Parameters[0].Name);
        Assert.Equal("FromSqlTest", command.Parameters[0].Value);
        Assert.Equal("@p1", command.Parameters[1].Name);
        Assert.Equal(1, command.Parameters[1].Value);

        List<Book> result = table.ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("FromSqlTest", result[0].Title);
        Assert.Equal(2, result[0].AuthorId);
        Assert.Equal(99, result[0].Price);
    }
}