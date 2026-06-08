using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiStatementExecuteParityTests
{
    [Fact]
    public void Execute_TwoStatements_RunsBoth()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Execute(
            "INSERT INTO \"Books\" (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (1, 'A', 1, 1.0); " +
            "INSERT INTO \"Books\" (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (2, 'B', 1, 2.0)");

        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public void Execute_BatchWithSelectThenDelete_RunsBoth()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.0 });

        db.Execute("SELECT \"BookId\" FROM \"Books\"; DELETE FROM \"Books\"");

        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public void Execute_MultiStatementSharedParameter_BindsOnlyWhereUsed()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.0 });

        db.Execute(
            "UPDATE \"Books\" SET \"BookPrice\" = @p WHERE \"BookId\" = 1; " +
            "UPDATE \"Books\" SET \"BookTitle\" = 'Z' WHERE \"BookId\" = 2",
            new SQLiteParameter { Name = "@p", Value = 7.0 });

        Assert.Equal(7.0, db.Table<Book>().Single(b => b.Id == 1).Price);
        Assert.Equal("Z", db.Table<Book>().Single(b => b.Id == 2).Title);
    }
}
