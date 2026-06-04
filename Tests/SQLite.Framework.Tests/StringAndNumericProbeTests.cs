using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("WhitespaceRows")]
file sealed class WhitespaceRow
{
    [Key]
    public int Id { get; set; }

    public string? Text { get; set; }
}

public class StringAndNumericProbeTests
{
    [Fact]
    public void StringCompareIgnoreCaseIsHonored()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "apple", AuthorId = 1, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => string.Compare(b.Title, "apple", StringComparison.OrdinalIgnoreCase) == 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public void TrimWithEmptyCharArrayTrimsWhitespace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "  Test  ", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Trim(new char[0]) == "Test")
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void TrimWithEmptyArrayInitializerDoesNotEmitMalformedSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "  Test  ", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Trim(new char[] { }) == "Test")
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void IsNullOrWhiteSpaceRecognizesTabsAndNewlines()
    {
        using TestDatabase db = new();
        db.Table<WhitespaceRow>().Schema.CreateTable();
        db.Table<WhitespaceRow>().Add(new WhitespaceRow { Id = 1, Text = "\t\t" });
        db.Table<WhitespaceRow>().Add(new WhitespaceRow { Id = 2, Text = "\n" });

        List<int> ids = db.Table<WhitespaceRow>()
            .Where(r => string.IsNullOrWhiteSpace(r.Text))
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public void FloatModuloKeepsFractionalPart()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5.5 });

        double result = db.Table<Book>().Select(b => b.Price % 2.0).First();

        Assert.Equal(1.5, result);
    }
}
