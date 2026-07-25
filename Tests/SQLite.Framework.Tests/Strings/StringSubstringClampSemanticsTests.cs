using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringSubstringClampSemanticsTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void SubstringLengthBeyondEndClampsToAvailable()
    {
        using TestDatabase db = Seed();

        string actual = db.Table<Book>().Select(x => x.Title.Substring(0, 5)).First();

        Assert.Equal("ab", actual);
    }

    [Fact]
    public void SubstringStartBeyondEndReturnsEmpty()
    {
        using TestDatabase db = Seed();

        string actual = db.Table<Book>().Select(x => x.Title.Substring(5)).First();

        Assert.Equal("", actual);
    }
}
