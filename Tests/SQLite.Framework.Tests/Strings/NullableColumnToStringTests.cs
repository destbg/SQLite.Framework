using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullToStringRows")]
public class NullToStringRow
{
    [Key]
    public int Id { get; set; }

    public bool? Flag { get; set; }

    public char? Letter { get; set; }

    public TimeSpan? Span { get; set; }

    public Guid? Token { get; set; }
}

public class NullableColumnToStringTests
{
    private static List<NullToStringRow> Rows()
    {
        return
        [
            new NullToStringRow
            {
                Id = 1,
                Flag = true,
                Letter = 'x',
                Span = TimeSpan.FromMinutes(90),
                Token = new Guid("11111111-2222-3333-4444-555555555555"),
            },
            new NullToStringRow { Id = 2 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullToStringRow>().Schema.CreateTable();
        db.Table<NullToStringRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void NullableBoolToStringReturnsEmptyForNull()
    {
        using TestDatabase db = Seed();

        List<string?> expected = Rows().OrderBy(r => r.Id).Select(r => r.Flag.ToString()).ToList();
        List<string?> actual = db.Table<NullToStringRow>().OrderBy(r => r.Id).Select(r => r.Flag.ToString()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableCharToStringReturnsEmptyForNull()
    {
        using TestDatabase db = Seed();

        List<string?> expected = Rows().OrderBy(r => r.Id).Select(r => r.Letter.ToString()).ToList();
        List<string?> actual = db.Table<NullToStringRow>().OrderBy(r => r.Id).Select(r => r.Letter.ToString()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableTimeSpanToStringReturnsEmptyForNull()
    {
        using TestDatabase db = Seed();

        List<string?> expected = Rows().OrderBy(r => r.Id).Select(r => r.Span.ToString()).ToList();
        List<string?> actual = db.Table<NullToStringRow>().OrderBy(r => r.Id).Select(r => r.Span.ToString()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableGuidToStringReturnsEmptyForNull()
    {
        using TestDatabase db = Seed();

        List<string?> expected = Rows().OrderBy(r => r.Id).Select(r => r.Token.ToString()).ToList();
        List<string?> actual = db.Table<NullToStringRow>().OrderBy(r => r.Id).Select(r => r.Token.ToString()).ToList();

        Assert.Equal(expected, actual);
    }
}
