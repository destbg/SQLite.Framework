using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("LikeRows")]
file sealed class LikeRow
{
    [Key]
    public int Id { get; set; }

    public string Value { get; set; } = "";

    public string Prefix { get; set; } = "";
}

[Table("TextRows")]
file sealed class TextRow
{
    [Key]
    public int Id { get; set; }

    public string? Value { get; set; }
}

public class StringTranslationTests
{
    [Fact]
    public void StartsWithNonConstantPatternTreatsWildcardsAsLiteral()
    {
        using TestDatabase db = new();
        db.Table<LikeRow>().Schema.CreateTable();
        List<LikeRow> data = new()
        {
            new LikeRow { Id = 1, Value = "100", Prefix = "10_" },
            new LikeRow { Id = 2, Value = "1_0", Prefix = "1_0" },
        };
        db.Table<LikeRow>().AddRange(data);

        List<int> expected = data.Where(r => r.Value.StartsWith(r.Prefix)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<LikeRow>().Where(r => r.Value.StartsWith(r.Prefix)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsWithOrdinalComparisonIsCaseSensitiveByDefault()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Clean Code", AuthorId = 1, Price = 1 });

        int expected = new[] { "Clean Code" }.Count(t => t.Contains("clean", StringComparison.Ordinal));
        int actual = db.Table<Book>().Where(b => b.Title.Contains("clean", StringComparison.Ordinal)).ToList().Count;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsWithOrdinalComparisonIsCaseSensitiveWhenOptionEnabled()
    {
        using TestDatabase db = new(b => b.UseCaseSensitiveStringComparison());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Clean Code", AuthorId = 1, Price = 1 });

        int actual = db.Table<Book>().Where(b => b.Title.Contains("clean", StringComparison.Ordinal)).ToList().Count;

        Assert.Equal(0, actual);
    }

    [Fact]
    public void IsNullOrWhiteSpaceTreatsUnicodeWhitespaceAsWhitespace()
    {
        using TestDatabase db = new();
        db.Table<TextRow>().Schema.CreateTable();
        List<TextRow> data = new()
        {
            new TextRow { Id = 1, Value = "\u00A0" },
            new TextRow { Id = 2, Value = "x" },
            new TextRow { Id = 3, Value = "\u2028" },
            new TextRow { Id = 4, Value = "\u3000\u2003" },
            new TextRow { Id = 5, Value = "a\u00A0b" },
        };
        db.Table<TextRow>().AddRange(data);

        List<int> expected = data.Where(r => string.IsNullOrWhiteSpace(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TextRow>().Where(r => string.IsNullOrWhiteSpace(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
