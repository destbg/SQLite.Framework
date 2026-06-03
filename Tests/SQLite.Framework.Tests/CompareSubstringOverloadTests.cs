using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CompareSubstringOverloadTests
{
    private static TestDatabase SeedTitle(string title)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void Compare_SubstringOverload_MatchesDotNetSign()
    {
        using TestDatabase db = SeedTitle("hello");

        int e1 = string.Compare("hello", 2, "fully", 2, 2);
        int e2 = string.Compare("hello", 0, "help", 0, 4);
        int e3 = string.Compare("hello", 1, "hello", 2, 2);
        int e4 = string.Compare("hello", 0, "abc", 0, 5);

        int a1 = db.Table<Book>().Select(b => string.Compare(b.Title, 2, "fully", 2, 2)).First();
        int a2 = db.Table<Book>().Select(b => string.Compare(b.Title, 0, "help", 0, 4)).First();
        int a3 = db.Table<Book>().Select(b => string.Compare(b.Title, 1, b.Title, 2, 2)).First();
        int a4 = db.Table<Book>().Select(b => string.Compare(b.Title, 0, "abc", 0, 5)).First();

        Assert.Equal(0, Math.Sign(e1));
        Assert.Equal(-1, Math.Sign(e2));
        Assert.Equal(-1, Math.Sign(e3));
        Assert.Equal(1, Math.Sign(e4));

        Assert.Equal(Math.Sign(e1), Math.Sign(a1));
        Assert.Equal(Math.Sign(e2), Math.Sign(a2));
        Assert.Equal(Math.Sign(e3), Math.Sign(a3));
        Assert.Equal(Math.Sign(e4), Math.Sign(a4));
    }

    [Fact]
    public void Compare_SubstringOverload_LengthClamps_MatchesDotNetSign()
    {
        using TestDatabase db = SeedTitle("ab");

        int expected = string.Compare("ab", 0, "abc", 0, 10);
        int actual = db.Table<Book>().Select(b => string.Compare(b.Title, 0, "abc", 0, 10)).First();

        Assert.Equal(-1, Math.Sign(expected));
        Assert.Equal(Math.Sign(expected), Math.Sign(actual));
    }

    [Fact]
    public void Compare_SubstringOverload_IgnoreCase_MatchesDotNetSign()
    {
        using TestDatabase db = SeedTitle("HELLO");

        int expected = string.Compare("HELLO", 0, "hello", 0, 5, true);
        int actual = db.Table<Book>().Select(b => string.Compare(b.Title, 0, "hello", 0, 5, true)).First();

        Assert.Equal(0, Math.Sign(expected));
        Assert.Equal(Math.Sign(expected), Math.Sign(actual));
    }

    [Fact]
    public void Compare_SubstringOverload_StringComparison_MatchesDotNetSign()
    {
        using TestDatabase db = SeedTitle("HELLO");

        int expected = string.Compare("HELLO", 0, "hello", 0, 5, StringComparison.OrdinalIgnoreCase);
        int actual = db.Table<Book>()
            .Select(b => string.Compare(b.Title, 0, "hello", 0, 5, StringComparison.OrdinalIgnoreCase))
            .First();

        Assert.Equal(0, Math.Sign(expected));
        Assert.Equal(Math.Sign(expected), Math.Sign(actual));
    }

    [Fact]
    public void Compare_TwoArg_StillMatchesDotNetSign()
    {
        using TestDatabase db = SeedTitle("hello");

        int expected = string.Compare("hello", "help");
        int actual = db.Table<Book>().Select(b => string.Compare(b.Title, "help")).First();

        Assert.Equal(-1, Math.Sign(expected));
        Assert.Equal(Math.Sign(expected), Math.Sign(actual));
    }

    [Fact]
    public void Compare_CultureOverload_Throws()
    {
        using TestDatabase db = SeedTitle("hello");

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => string.Compare(b.Title, 0, "hello", 0, 5, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.CompareOptions.None))
                .First());
    }
}
