using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MethodVisitorCoverageTests
{
    [Fact]
    public void StringEquals_WithOrdinalComparison_ProducesExactMatchSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => string.Equals(b.Title, "Test", StringComparison.Ordinal))
            .ToSqlCommand();

        Assert.Contains("= @p0", command.CommandText);
        Assert.DoesNotContain("COLLATE NOCASE", command.CommandText);
    }

    [Fact]
    public void StringTrim_WithSingleCharArg_TrimsCharFromBothEnds()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "xTestx",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.Trim('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void StringTrimStart_WithSingleCharArg_TrimsCharFromStart()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "xTest",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.TrimStart('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void StringTrimEnd_WithSingleCharArg_TrimsCharFromEnd()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Testx",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.TrimEnd('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_AddDaysInWhere_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<DateTimeOffsetMethodEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetMethodEntity>()
                .Where(e => e.Date.AddDays(1) > DateTimeOffset.Now)
                .ToList());
    }

    [Fact]
    public void TimeSpan_Text_AddInWhere_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeSpanStorage = TimeSpanStorageMode.Text;
        });
        db.Table<TimeSpanMethodEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanMethodEntity>()
                .Where(e => e.Duration.Add(TimeSpan.FromHours(1)) > TimeSpan.FromHours(2))
                .ToList());
    }

    [Fact]
    public void IntToString_OnColumn_CastsAsText()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 42,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        string? result = db.Table<Book>()
            .Select(b => b.Id.ToString())
            .First();

        Assert.Equal("42", result);
    }

    [Fact]
    public void FloatToString_OnColumn_CastsAsText()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Select(b => b.Price.ToString())
            .ToSqlCommand();

        Assert.Contains("CAST", command.CommandText);
        Assert.Contains("AS TEXT", command.CommandText);
    }

    [Fact]
    public void FloatParse_OnColumn_CastsAsReal()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => float.Parse(b.Title) > 10.0f)
            .ToSqlCommand();

        Assert.Contains("CAST", command.CommandText);
        Assert.Contains("AS REAL", command.CommandText);
    }

    private class DateTimeOffsetMethodEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset Date { get; set; }
    }

    private class TimeSpanMethodEntity
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan Duration { get; set; }
    }
}