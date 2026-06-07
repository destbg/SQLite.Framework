using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CovDoubleRows")]
file sealed class CovDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double? D { get; set; }
}

[Table("CovTemporalRows")]
file sealed class CovTemporalRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset Dto { get; set; }

    public TimeSpan Ts { get; set; }
}

public class SupplementalBehaviorTests
{
    [Fact]
    public void BooleanBitwiseOrInsideAndIsBracketed()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.Table<Book>()
            .Where(b => (b.Id == 1 | b.Id == 2) && b.Price > 10)
            .ToSqlCommand();

        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE (b0.\"BookId\" = @p0 OR b0.\"BookId\" = @p1) AND b0.\"BookPrice\" > @p2", command.CommandText);
    }

    [Fact]
    public void NotOverEachCompoundShape()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });

        Assert.NotEmpty(db.Table<Book>().Where(b => !(b.Id == 9 || b.Id == 8)).ToList());
        Assert.NotEmpty(db.Table<Book>().Where(b => !(b.Id == 9 | b.Id == 8)).ToList());
        Assert.NotEmpty(db.Table<Book>().Where(b => !(b.Id == 9 & b.Price > 99)).ToList());
        Assert.NotEmpty(db.Table<Book>().Where(b => !(b.Id == 9)).ToList());
    }

    [Fact]
    public void NullableDoubleModuloTranslates()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.Table<CovDoubleRow>().Select(r => r.D % 2.0).ToSqlCommand();

        Assert.Equal("SELECT (s0.\"D\" - @p0 * CAST(s0.\"D\" / @p0 AS INTEGER)) AS \"4\"\nFROM \"CovDoubleRows\" AS s0", command.CommandText);
    }

    [Fact]
    public void ProjectionWithBoolConstantSignature()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>().Select(b => new { b.Id, Flag = true }).ToList();

        Assert.True(rows[0].Flag);
    }

    [Fact]
    public void IndexOfWithStringComparisonThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => b.Title.IndexOf("x", StringComparison.Ordinal) == 0).ToList());
    }

    [Fact]
    public void CompareWithBoolIgnoreCaseIsHonored()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => string.Compare(b.Title, "apple", true) == 0)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void CaseSensitiveContainsWithNonConstantValue()
    {
        using TestDatabase db = new(b => b.UseCaseSensitiveStringComparison());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Contains(b.Title))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void InsertFromQueryWholeEntityUsesTableColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        int inserted = db.Table<Book>().InsertFromQuery(db.Table<Book>().Where(b => b.Id > 1000000));

        Assert.Equal(0, inserted);
    }

    [Fact]
    public void DateTimeOffsetAndTimeSpanCustomFormatsRoundTrip()
    {
        using TestDatabase db = new(b => b
            .UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.TextFormatted, "yyyy-MM-dd HH:mm:ss zzz")
            .UseTimeSpanStorage(TimeSpanStorageMode.Text, "c"));
        db.Table<CovTemporalRow>().Schema.CreateTable();
        db.Table<CovTemporalRow>().Add(new CovTemporalRow
        {
            Id = 1,
            Dto = new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.FromHours(2)),
            Ts = TimeSpan.FromMinutes(125)
        });

        CovTemporalRow back = db.Table<CovTemporalRow>().First();

        Assert.Equal(new DateTimeOffset(2000, 2, 3, 4, 5, 6, TimeSpan.FromHours(2)), back.Dto);
        Assert.Equal(TimeSpan.FromMinutes(125), back.Ts);
    }

    [Fact]
    public void FullTextMatchOnNonFtsEntityThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => SQLiteFTS5Functions.Match(b.Title, "x")).ToList());
    }
}
