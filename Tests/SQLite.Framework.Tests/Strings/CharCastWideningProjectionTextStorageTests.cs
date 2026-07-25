using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharWideningRow
{
    [Key]
    public int Id { get; set; }

    public char Ch { get; set; }
}

public class CharCastWideningProjectionTextStorageTests
{
    private static List<CharWideningRow> Rows() =>
    [
        new() { Id = 1, Ch = 'z' },
        new() { Id = 2, Ch = 'A' },
        new() { Id = 3, Ch = 'm' },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.CharStorage = CharStorageMode.Text);
        db.Table<CharWideningRow>().Schema.CreateTable();
        db.Table<CharWideningRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CharCastToLongProjection()
    {
        using TestDatabase db = Seed();

        List<long> expected = Rows().OrderBy(r => r.Id).Select(r => (long)r.Ch).ToList();
        Assert.Equal([122L, 65L, 109L], expected);

        List<long> actual = db.Table<CharWideningRow>().OrderBy(r => r.Id).Select(r => (long)r.Ch).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharCastToShortProjection()
    {
        using TestDatabase db = Seed();

        List<short> expected = Rows().OrderBy(r => r.Id).Select(r => (short)r.Ch).ToList();
        Assert.Equal([(short)122, (short)65, (short)109], expected);

        List<short> actual = db.Table<CharWideningRow>().OrderBy(r => r.Id).Select(r => (short)r.Ch).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharCastToDoubleProjection()
    {
        using TestDatabase db = Seed();

        List<double> expected = Rows().OrderBy(r => r.Id).Select(r => (double)r.Ch).ToList();
        Assert.Equal([122d, 65d, 109d], expected);

        List<double> actual = db.Table<CharWideningRow>().OrderBy(r => r.Id).Select(r => (double)r.Ch).ToList();
        Assert.Equal(expected, actual);
    }
}
