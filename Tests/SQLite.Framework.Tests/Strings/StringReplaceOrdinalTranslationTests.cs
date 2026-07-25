using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringReplaceOrdinalTranslationTests
{
    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<CasedTextRow>().Schema.CreateTable();
        db.Table<CasedTextRow>().Add(new CasedTextRow { Id = 1, Name = "AbcABC" });
        return db;
    }

    [Fact]
    public void ReplaceWithOrdinalComparisonTranslates()
    {
        using TestDatabase db = Seed(nameof(ReplaceWithOrdinalComparisonTranslates));

        int expected = "AbcABC".Replace("ABC", "x", StringComparison.Ordinal) == "Abcx" ? 1 : 0;
        int actual = db.Table<CasedTextRow>().Count(x => x.Name.Replace("ABC", "x", StringComparison.Ordinal) == "Abcx");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReplaceWithIgnoreCaseAndCultureMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ReplaceWithIgnoreCaseAndCultureMatchesLinq));

        string expected = "AbcABC".Replace("abc", "x", true, CultureInfo.InvariantCulture);
        string actual = db.Table<CasedTextRow>().Select(x => x.Name.Replace("abc", "x", true, CultureInfo.InvariantCulture)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReplaceWithCaseSensitiveCultureMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ReplaceWithCaseSensitiveCultureMatchesLinq));

        string expected = "AbcABC".Replace("ABC", "x", false, CultureInfo.InvariantCulture);
        string actual = db.Table<CasedTextRow>().Select(x => x.Name.Replace("ABC", "x", false, CultureInfo.InvariantCulture)).First();

        Assert.Equal(expected, actual);
    }
}
