using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteCommandTests
{
    [Fact]
    public void ParameterlessCtor_HasEmptyDefaults()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = new(db);

        Assert.Equal(string.Empty, cmd.CommandText);
        Assert.Empty(cmd.Parameters);
    }

    [Fact]
    public void ExecuteNonQuery_MalformedSql_Throws()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("NOT VALID SQL", []);

        Assert.Throws<SQLiteException>(() => cmd.ExecuteNonQuery());
    }

    [Fact]
    public void ExecuteReader_MalformedSql_Throws()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT * FROM NoSuchTable", []);

        Assert.Throws<SQLiteException>(() => cmd.ExecuteReader());
    }

    [Fact]
    public void ExecuteWithLastRowId_MalformedSql_Throws()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("INSERT INTO NoSuchTable (X) VALUES (1)", []);

        Assert.Throws<SQLiteException>(() => cmd.ExecuteWithLastRowId());
    }

    [Fact]
    public void ExecuteWithLastRowId_Insert_ReturnsChangesAndRowId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        SQLiteCommand cmd = db.CreateCommand(
            "INSERT INTO Article (Title, Body, PublishedAt) VALUES (@t, @b, @p)",
            [
                new SQLiteParameter { Name = "@t", Value = "title" },
                new SQLiteParameter { Name = "@b", Value = "body" },
                new SQLiteParameter { Name = "@p", Value = DateTime.UtcNow.Ticks }
            ]);

        (int changes, long rowId) = cmd.ExecuteWithLastRowId();

        Assert.Equal(1, changes);
        Assert.True(rowId > 0);
    }

    [Fact]
    public void ExecuteWithLastRowId_ConstraintViolation_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 0, Price = 1 });

        SQLiteCommand cmd = db.CreateCommand(
            "INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'b', 0, 2)",
            []);

        Assert.Throws<SQLiteException>(() => cmd.ExecuteWithLastRowId());
    }

    [Fact]
    public void Mutators_OnCommandTextAndParameters_Apply()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        SQLiteCommand cmd = new(db);
        cmd.CommandText = "INSERT INTO Article (Title, Body, PublishedAt) VALUES (@t, @b, @p)";
        cmd.Parameters.Add(new SQLiteParameter { Name = "@t", Value = "x" });
        cmd.Parameters.Add(new SQLiteParameter { Name = "@b", Value = "y" });
        cmd.Parameters.Add(new SQLiteParameter { Name = "@p", Value = DateTime.UtcNow.Ticks });

        Assert.Equal(1, cmd.ExecuteNonQuery());
    }
}
