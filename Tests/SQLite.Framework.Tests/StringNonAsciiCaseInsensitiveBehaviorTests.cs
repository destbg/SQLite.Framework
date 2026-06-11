using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringNonAsciiCaseInsensitiveBehaviorTests
{
    private static TestDatabase SeedTitles(params string[] titles)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 0; i < titles.Length; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = titles[i], AuthorId = 1, Price = 1 });
        }
        return db;
    }

    [Fact]
    public void InstanceEquals_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("Ä");

        Assert.True("Ä".Equals("ä", StringComparison.OrdinalIgnoreCase));
        bool actual = db.Table<Book>().Select(b => b.Title.Equals("ä", StringComparison.OrdinalIgnoreCase)).First();

        Assert.False(actual);
    }

    [Fact]
    public void StaticCompare_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("Ä");

        Assert.Equal(0, string.Compare("Ä", "ä", StringComparison.OrdinalIgnoreCase));
        int actual = db.Table<Book>().Select(b => string.Compare(b.Title, "ä", StringComparison.OrdinalIgnoreCase)).First();

        Assert.Equal(-1, actual);
    }

    [Fact]
    public void StaticEquals_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("Ä");

        Assert.True(string.Equals("Ä", "ä", StringComparison.OrdinalIgnoreCase));
        bool actual = db.Table<Book>().Select(b => string.Equals(b.Title, "ä", StringComparison.OrdinalIgnoreCase)).First();

        Assert.False(actual);
    }

    [Fact]
    public void Contains_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("strAÄße");

        Assert.True("strAÄße".Contains("aä", StringComparison.OrdinalIgnoreCase));
        bool actual = db.Table<Book>().Where(b => b.Title.Contains("aä", StringComparison.OrdinalIgnoreCase)).Any();

        Assert.False(actual);
    }

    [Fact]
    public void StartsWith_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("Äpfel");

        Assert.True("Äpfel".StartsWith("ä", StringComparison.OrdinalIgnoreCase));
        bool actual = db.Table<Book>().Where(b => b.Title.StartsWith("ä", StringComparison.OrdinalIgnoreCase)).Any();

        Assert.False(actual);
    }

    [Fact]
    public void EndsWith_NonAsciiPair_OrdinalIgnoreCase_FoldsOnlyAscii()
    {
        using TestDatabase db = SeedTitles("straÄ");

        Assert.True("straÄ".EndsWith("ä", StringComparison.OrdinalIgnoreCase));
        bool actual = db.Table<Book>().Where(b => b.Title.EndsWith("ä", StringComparison.OrdinalIgnoreCase)).Any();

        Assert.False(actual);
    }

    [Fact]
    public void Contains_AsciiPair_OrdinalIgnoreCase_MatchesDotNet()
    {
        using TestDatabase db = SeedTitles("Apple");

        bool oracle = "Apple".Contains("APP", StringComparison.OrdinalIgnoreCase);
        bool actual = db.Table<Book>().Where(b => b.Title.Contains("APP", StringComparison.OrdinalIgnoreCase)).Any();

        Assert.Equal(oracle, actual);
    }
}
