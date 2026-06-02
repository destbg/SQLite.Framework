using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests.BugHunt;

public class ScalarNullMaterializationBugTests
{
    [Fact]
    public void NullifScalarToListReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 1.0 });

        List<int> actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).ToList();

        Assert.Equal(new List<int> { 0 }, actual);
    }

    [Fact]
    public void NullifScalarFirstReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 1.0 });

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void NullifScalarFirstOrDefaultReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 1.0 });

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).FirstOrDefault();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void NullifScalarSingleReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 1.0 });

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).Single();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void DivByZeroScalarFirstReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 0, Price = 1.0 });

        int actual = db.Table<Book>().Select(b => b.Id / b.AuthorId).First();

        Assert.Equal(0, actual);
    }
}
