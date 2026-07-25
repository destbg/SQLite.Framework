using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LastIndexOfWithStartIndexTests
{
    private static TestDatabase SeedTitle(string title)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void LastIndexOf_StringWithStartIndex_MatchesDotNet()
    {
        using TestDatabase db = SeedTitle("banana");

        (string needle, int start)[] cases =
        [
            ("a", 3), ("an", 3), ("an", 4), ("ana", 4), ("na", 5), ("x", 3),
            ("", 3), ("", 0), ("a", 0), ("b", 0), ("ba", 0), ("a", 5), ("a", 6),
        ];

        foreach ((string needle, int start) in cases)
        {
            int expected = "banana".LastIndexOf(needle, start);
            int actual = db.Table<Book>().Select(b => b.Title.LastIndexOf(needle, start)).First();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void LastIndexOf_HeadlineCases_PinnedValues()
    {
        using TestDatabase db = SeedTitle("banana");

        Assert.Equal(3, "banana".LastIndexOf("a", 3));
        Assert.Equal(1, "banana".LastIndexOf("an", 3));
        Assert.Equal(-1, "banana".LastIndexOf("a", 0));

        Assert.Equal(3, db.Table<Book>().Select(b => b.Title.LastIndexOf("a", 3)).First());
        Assert.Equal(1, db.Table<Book>().Select(b => b.Title.LastIndexOf("an", 3)).First());
        Assert.Equal(-1, db.Table<Book>().Select(b => b.Title.LastIndexOf("a", 0)).First());
    }

    [Fact]
    public void LastIndexOf_CharWithStartIndex_MatchesDotNet()
    {
        using TestDatabase db = SeedTitle("banana");

        (char needle, int start)[] cases = [('a', 3), ('a', 0), ('n', 2), ('a', 5), ('x', 4)];

        foreach ((char needle, int start) in cases)
        {
            int expected = "banana".LastIndexOf(needle, start);
            int actual = db.Table<Book>().Select(b => b.Title.LastIndexOf(needle, start)).First();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void LastIndexOf_SingleArg_StillMatchesDotNet()
    {
        using TestDatabase db = SeedTitle("banana");

        int expected = "banana".LastIndexOf("a");
        int actual = db.Table<Book>().Select(b => b.Title.LastIndexOf("a")).First();

        Assert.Equal(5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOf_CountOverload_ClientEvaluates()
    {
        using TestDatabase db = SeedTitle("banana");

        int expected = "banana".LastIndexOf("a", 5, 3);
        int actual = db.Table<Book>().Select(b => b.Title.LastIndexOf("a", 5, 3)).First();

        Assert.Equal(5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOf_StringComparisonOverload_ClientEvaluates()
    {
        using TestDatabase db = SeedTitle("banana");

        int expected = "banana".LastIndexOf("a", StringComparison.Ordinal);
        int actual = db.Table<Book>().Select(b => b.Title.LastIndexOf("a", StringComparison.Ordinal)).First();

        Assert.Equal(5, expected);
        Assert.Equal(expected, actual);
    }
}
