using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TrimCapturedCharArrayParityTests
{
    private static TestDatabase CreateDb(string title)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void Trim_CapturedCharArray_MatchesDotNet()
    {
        using TestDatabase db = CreateDb("xhellox");
        char[] trimChars = ['x'];

        string expected = "xhellox".Trim(trimChars);
        string actual = db.Table<Book>().Select(b => b.Title.Trim(trimChars)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStart_CapturedCharArray_MatchesDotNet()
    {
        using TestDatabase db = CreateDb("xhellox");
        char[] trimChars = ['x'];

        string expected = "xhellox".TrimStart(trimChars);
        string actual = db.Table<Book>().Select(b => b.Title.TrimStart(trimChars)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimEnd_CapturedCharArray_MatchesDotNet()
    {
        using TestDatabase db = CreateDb("xhellox");
        char[] trimChars = ['x'];

        string expected = "xhellox".TrimEnd(trimChars);
        string actual = db.Table<Book>().Select(b => b.Title.TrimEnd(trimChars)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_CapturedMultiCharArray_MatchesDotNet()
    {
        using TestDatabase db = CreateDb("axbhelloaxb");
        char[] trimChars = ['a', 'x', 'b'];

        string expected = "axbhelloaxb".Trim(trimChars);
        string actual = db.Table<Book>().Select(b => b.Title.Trim(trimChars)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_CapturedNullCharArray_TrimsWhitespace()
    {
        using TestDatabase db = CreateDb(" hi ");
        char[]? trimChars = null;

        string expected = " hi ".Trim(trimChars!);
        string actual = db.Table<Book>().Select(b => b.Title.Trim(trimChars!)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_CapturedEmptyCharArray_TrimsWhitespace()
    {
        using TestDatabase db = CreateDb(" hi ");
        char[] trimChars = [];

        string expected = " hi ".Trim(trimChars);
        string actual = db.Table<Book>().Select(b => b.Title.Trim(trimChars)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_CapturedCharArray_OnConcatExpression_MatchesDotNet()
    {
        using TestDatabase db = CreateDb("xhello");
        char[] trimChars = ['x'];
        string suffix = "xx";

        string expected = ("xhello" + suffix).Trim(trimChars);
        string actual = db.Table<Book>().Select(b => (b.Title + suffix).Trim(trimChars)).First();

        Assert.Equal(expected, actual);
    }
}
