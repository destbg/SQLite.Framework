using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ScalarNullMaterializationTests
{
    private static TestDatabase SeedOne(int authorId)
    {
        TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = authorId, Price = 1.0 });
        return db;
    }

    [Fact]
    public void NullScalarToListReturnsDefault()
    {
        using TestDatabase db = SeedOne(1);

        List<int> actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).ToList();

        Assert.Equal(new List<int> { 0 }, actual);
    }

    [Fact]
    public void NullScalarFirstReturnsDefault()
    {
        using TestDatabase db = SeedOne(1);

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void NullScalarFirstOrDefaultReturnsDefault()
    {
        using TestDatabase db = SeedOne(1);

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).FirstOrDefault();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void NullScalarSingleReturnsDefault()
    {
        using TestDatabase db = SeedOne(1);

        int actual = db.Table<Book>().Select(b => SQLiteFunctions.Nullif(b.AuthorId, 1)).Single();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void DivisionByZeroScalarFirstReturnsDefault()
    {
        using TestDatabase db = SeedOne(0);

        int actual = db.Table<Book>().Select(b => b.Id / b.AuthorId).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void NullScalarIntoNullableFirstReturnsNull()
    {
        using TestDatabase db = SeedOne(1);

        int? actual = db.Table<Book>().Select(b => (int?)SQLiteFunctions.Nullif(b.AuthorId, 1)).First();

        Assert.Null(actual);
    }
}
