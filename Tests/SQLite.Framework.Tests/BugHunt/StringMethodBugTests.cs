using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class StringMethodBugTests
{
    [Fact]
    public void LastIndexOf_WithStartIndex_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "banana", AuthorId = 1, Price = 1 });

        int expectedString = "banana".LastIndexOf("a", 3);
        int expectedChar = "banana".LastIndexOf('a', 3);
        int actualString = db.Table<Book>().Select(b => b.Title.LastIndexOf("a", 3)).First();
        int actualChar = db.Table<Book>().Select(b => b.Title.LastIndexOf('a', 3)).First();

        Assert.Equal(expectedString, actualString);
        Assert.Equal(expectedChar, actualChar);
    }

    [Fact]
    public void Compare_SubstringOverload_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        int expected = string.Compare("hello", 2, "fully", 2, 2);
        int actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => string.Compare(b.Title, 2, "fully", 2, 2))
            .First();

        Assert.Equal(Math.Sign(expected), Math.Sign(actual));
    }
}
