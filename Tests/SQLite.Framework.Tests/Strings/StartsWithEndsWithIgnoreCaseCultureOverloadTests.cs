using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TitledRow
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }
}

public class StartsWithEndsWithIgnoreCaseCultureOverloadTests
{
    private static List<TitledRow> Rows() =>
    [
        new() { Id = 1, Title = "Apple pie" },
    ];

    private static TestDatabase Seed(bool caseSensitive)
    {
        TestDatabase db = new(b => b.UseCaseSensitiveStringComparison(caseSensitive));
        db.Table<TitledRow>().Schema.CreateTable();
        db.Table<TitledRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void StartsWithIgnoreCaseFalseStaysCaseSensitive()
    {
        using TestDatabase db = Seed(caseSensitive: false);

        List<int> expected = Rows()
            .Where(r => r.Title.StartsWith("APP", false, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal([], expected);

        List<int> actual = db.Table<TitledRow>()
            .Where(r => r.Title.StartsWith("APP", false, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EndsWithIgnoreCaseFalseStaysCaseSensitive()
    {
        using TestDatabase db = Seed(caseSensitive: false);

        List<int> expected = Rows()
            .Where(r => r.Title.EndsWith("PIE", false, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal([], expected);

        List<int> actual = db.Table<TitledRow>()
            .Where(r => r.Title.EndsWith("PIE", false, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StartsWithIgnoreCaseTrueMatchesOtherCasing()
    {
        using TestDatabase db = Seed(caseSensitive: true);

        List<int> expected = Rows()
            .Where(r => r.Title.StartsWith("apple", true, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<TitledRow>()
            .Where(r => r.Title.StartsWith("apple", true, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EndsWithIgnoreCaseTrueMatchesOtherCasing()
    {
        using TestDatabase db = Seed(caseSensitive: true);

        List<int> expected = Rows()
            .Where(r => r.Title.EndsWith("PIE", true, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<TitledRow>()
            .Where(r => r.Title.EndsWith("PIE", true, CultureInfo.InvariantCulture))
            .Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }
}
