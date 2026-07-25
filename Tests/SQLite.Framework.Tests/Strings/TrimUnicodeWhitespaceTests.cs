using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TrimUnicodeWhitespaceTests
{
    private static readonly string[] WhitespaceWrapped =
    [
        "\u0009Test\u000a",
        "  Test  ",
        " Test ",
        "\u3000Test\u3000",
        "Test",
        "\u000d\u000aTest\u000d\u000a",
        "\u00a0Test\u00a0",
        "\u2028Test\u2029",
    ];

    private static TestDatabase SeedTitles(params string[] titles)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 0; i < titles.Length; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = titles[i], AuthorId = 1, Price = i + 1 });
        }

        return db;
    }

    [Fact]
    public void Trim_StripsUnicodeWhitespace_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles(WhitespaceWrapped);

        List<string> expected = WhitespaceWrapped.Select(s => s.Trim()).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.Trim()).ToList();

        Assert.All(expected, e => Assert.Equal("Test", e));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStart_StripsUnicodeWhitespace_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles(WhitespaceWrapped);

        List<string> expected = WhitespaceWrapped.Select(s => s.TrimStart()).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.TrimStart()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimEnd_StripsUnicodeWhitespace_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles(WhitespaceWrapped);

        List<string> expected = WhitespaceWrapped.Select(s => s.TrimEnd()).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.TrimEnd()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_EmptyCharArray_StripsUnicodeWhitespace_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles("\u0009Test ");

        string expectedInit = "\u0009Test ".Trim(new char[] { });
        string expectedBounds = "\u0009Test ".Trim(new char[0]);
        string actualInit = db.Table<Book>().Select(b => b.Title.Trim(new char[] { })).Single();
        string actualBounds = db.Table<Book>().Select(b => b.Title.Trim(new char[0])).Single();

        Assert.Equal("Test", expectedInit);
        Assert.Equal(expectedInit, actualInit);
        Assert.Equal(expectedBounds, actualBounds);
    }

    [Fact]
    public void Trim_DoesNotStripNonWhitespace_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles("xxTestxx");

        string expected = "xxTestxx".Trim();
        string actual = db.Table<Book>().Select(b => b.Title.Trim()).Single();

        Assert.Equal("xxTestxx", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsNullOrWhiteSpace_StillMatchesDotNet()
    {
        string[] titles = ["\u0009\u000a  ", "Test", " a "];
        using TestDatabase db = SeedTitles(titles);

        List<bool> expected = titles.Select(string.IsNullOrWhiteSpace).ToList();
        List<bool> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => string.IsNullOrWhiteSpace(b.Title)).ToList();

        Assert.Equal([true, false, false], expected);
        Assert.Equal(expected, actual);
    }
}
