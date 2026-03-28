using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FromSqlTests
{
    [Fact]
    public void FromSqlWithoutParameters_GeneratesSubquery()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithSingleParameter_GeneratesSubqueryWithParameter()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";
        SQLiteCommand command = db.FromSql<Book>(sql, new SQLiteParameter
        {
            Name = "@title",
            Value = "Test"
        }).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("@title", command.Parameters[0].Name);
        Assert.Equal("Test", command.Parameters[0].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithMultipleParameters_GeneratesSubqueryWithParameters()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title AND \"BookAuthorId\" = @authorId";
        SQLiteCommand command = db.FromSql<Book>(sql,
            new SQLiteParameter
            {
                Name = "@title",
                Value = "Test"
            },
            new SQLiteParameter
            {
                Name = "@authorId",
                Value = 5
            }
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("@title", command.Parameters[0].Name);
        Assert.Equal("Test", command.Parameters[0].Value);
        Assert.Equal("@authorId", command.Parameters[1].Name);
        Assert.Equal(5, command.Parameters[1].Value);
    }

    [Fact]
    public void FromSqlWithWhere_WrapsInSubqueryAndAddsWhereClause()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql)
            .Where(b => b.Price < 30)
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(30d, command.Parameters[0].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      WHERE b0.BookPrice < @p1
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithOrderBy_WrapsInSubqueryAndAddsOrderBy()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql)
            .OrderBy(b => b.Title)
            .ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      ORDER BY b0.BookTitle ASC
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithTake_WrapsInSubqueryAndAddsLimit()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql)
            .Take(5)
            .ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      LIMIT 5
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithSkip_WrapsInSubqueryAndAddsOffset()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql)
            .Skip(10)
            .ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      LIMIT -1
                      OFFSET 10
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlWithWhereOrderByTake_ChainsCorrectly()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand command = db.FromSql<Book>(sql)
            .Where(b => b.Price < 30)
            .OrderBy(b => b.Title)
            .Take(10)
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(30d, command.Parameters[0].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      WHERE b0.BookPrice < @p1
                      ORDER BY b0.BookTitle ASC
                      LIMIT 10
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromSqlOnTable_GeneratesSameQueryAsOnDatabase()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\"";
        SQLiteCommand dbCommand = db.FromSql<Book>(sql).ToSqlCommand();
        SQLiteCommand tableCommand = db.Table<Book>().FromSql(sql).ToSqlCommand();

        Assert.Equal(dbCommand.CommandText, tableCommand.CommandText);
        Assert.Equal(dbCommand.Parameters.Count, tableCommand.Parameters.Count);
    }

    [Fact]
    public void FromSqlOnTableWithParameter_MatchesDatabaseFromSql()
    {
        using TestDatabase db = new();

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";
        SQLiteParameter param = new()
        {
            Name = "@title",
            Value = "Test"
        };

        SQLiteCommand dbCommand = db.FromSql<Book>(sql, param).ToSqlCommand();
        SQLiteCommand tableCommand = db.Table<Book>().FromSql(sql, param).ToSqlCommand();

        Assert.Equal(dbCommand.CommandText, tableCommand.CommandText);
        Assert.Equal(dbCommand.Parameters.Count, tableCommand.Parameters.Count);
    }

    [Fact]
    public void FromSqlWithEmptySql_ThrowsArgumentException()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.FromSql<Book>(""));
    }

    [Fact]
    public void FromSqlWithWhitespaceSql_ThrowsArgumentException()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.FromSql<Book>("   "));
    }

    [Fact]
    public void FromSqlExecutesAndReturnsResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Alpha",
            AuthorId = 1,
            Price = 10
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "Beta",
            AuthorId = 1,
            Price = 20
        });

        List<Book> result = db.FromSql<Book>("SELECT * FROM \"Books\"").ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FromSqlWithParameterExecutesAndFiltersResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Alpha",
            AuthorId = 1,
            Price = 10
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "Beta",
            AuthorId = 1,
            Price = 20
        });

        List<Book> result = db.FromSql<Book>(
            "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title",
            new SQLiteParameter
            {
                Name = "@title",
                Value = "Alpha"
            }
        ).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Title);
    }

    [Fact]
    public void FromSqlWithChainedWhereExecutesAndFiltersResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Alpha",
            AuthorId = 1,
            Price = 10
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "Beta",
            AuthorId = 1,
            Price = 20
        });
        db.Table<Book>().Add(new Book
        {
            Id = 3,
            Title = "Gamma",
            AuthorId = 1,
            Price = 30
        });

        List<Book> result = db.FromSql<Book>("SELECT * FROM \"Books\"")
            .Where(b => b.Price < 25)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.Price < 25));
    }

    [Fact]
    public void FromSqlWithChainedOrderByExecutesAndOrdersResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Gamma",
            AuthorId = 1,
            Price = 30
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "Alpha",
            AuthorId = 1,
            Price = 10
        });
        db.Table<Book>().Add(new Book
        {
            Id = 3,
            Title = "Beta",
            AuthorId = 1,
            Price = 20
        });

        List<Book> result = db.FromSql<Book>("SELECT * FROM \"Books\"")
            .OrderBy(b => b.Title)
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Title);
        Assert.Equal("Beta", result[1].Title);
        Assert.Equal("Gamma", result[2].Title);
    }

    [Fact]
    public void FromSqlWithChainedTakeExecutesAndLimitsResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Alpha",
            AuthorId = 1,
            Price = 10
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "Beta",
            AuthorId = 1,
            Price = 20
        });
        db.Table<Book>().Add(new Book
        {
            Id = 3,
            Title = "Gamma",
            AuthorId = 1,
            Price = 30
        });

        List<Book> result = db.FromSql<Book>("SELECT * FROM \"Books\"")
            .OrderBy(b => b.Id)
            .Take(2)
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FromSqlWithFewerColumnsThanModel_ThrowsSQLiteException()
    {
        using TestDatabase db = new();

        db.Table<SimpleEntity>().CreateTable();
        db.Table<SimpleEntity>().Add(new SimpleEntity
        {
            Id = 1,
            Title = "Alpha",
            Author = "Author1"
        });

        SQLiteException ex = Assert.Throws<SQLiteException>(() =>
            db.FromSql<SimpleEntity>("SELECT \"Id\", \"Title\" FROM \"SimpleEntity\"").ToList()
        );

        Assert.Contains("no such column: s0.Author", ex.Message);
        Assert.Contains("SELECT", ex.Sql);
    }

    public class SimpleEntity
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Author { get; set; }
    }
}