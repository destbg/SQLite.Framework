using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
namespace SQLite.Framework.Tests;

public class AsyncCommandTests
{
    [Fact]
    public async Task ExecuteReaderAsync_ReadsRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        SQLiteCommand cmd = db.CreateCommand("SELECT BookId FROM Books", []);
        using SQLiteDataReader reader = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        Assert.True(reader.Read());
        Assert.Equal(1, (int)reader.GetValue(0, SQLiteColumnType.Integer, typeof(int))!);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_RunsInsert()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.CreateCommand(
            "INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@id, @title, @authorId, @price)",
            [
                new SQLiteParameter { Name = "@id", Value = 1 },
                new SQLiteParameter { Name = "@title", Value = "A" },
                new SQLiteParameter { Name = "@authorId", Value = 1 },
                new SQLiteParameter { Name = "@price", Value = 1.0 },
            ]);

        int affected = await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, affected);
        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_BadSql_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.CreateCommand("INSERT INTO Books (BookId) VALUES (NULL)", []);

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExecuteWithLastRowIdAsync_ReturnsRowId()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.CreateCommand(
            "INSERT INTO Books (BookTitle, BookAuthorId, BookPrice) VALUES (@title, @authorId, @price)",
            [
                new SQLiteParameter { Name = "@title", Value = "A" },
                new SQLiteParameter { Name = "@authorId", Value = 1 },
                new SQLiteParameter { Name = "@price", Value = 1.0 },
            ]);

        (int changes, long rowId) = await cmd.ExecuteWithLastRowIdAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, changes);
        Assert.True(rowId > 0);
    }

    [Fact]
    public async Task ExecuteWithLastRowIdAsync_BadSql_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.CreateCommand("INSERT INTO Books (BookId) VALUES (NULL)", []);

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await cmd.ExecuteWithLastRowIdAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExecuteReaderAsync_BadSql_ReleasesLockAndThrows()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT * FROM definitely_not_a_real_table", []);

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken));

        using IDisposable _ = db.Lock();
    }
}
#endif
