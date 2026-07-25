using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum NarrowSourceColor
{
    Red = 1
}

internal sealed class EnumNarrowingRow
{
    [Key]
    public int Id { get; set; }

    public NarrowSourceColor Color { get; set; }

    public int Number { get; set; }
}

public class EnumNarrowingCastWrapTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<EnumNarrowingRow>().Schema.CreateTable();
        db.Table<EnumNarrowingRow>().Add(new EnumNarrowingRow { Id = 1, Color = (NarrowSourceColor)300, Number = 42 });
        return db;
    }

    [Fact]
    public void ByteCastOfEnumColumnWraps()
    {
        using TestDatabase db = SetupDatabase();

        byte expected = db.Table<EnumNarrowingRow>().AsEnumerable()
            .Select(r => unchecked((byte)r.Color))
            .First();

        Assert.Equal(44, expected);

        byte actual = db.Table<EnumNarrowingRow>()
            .Select(r => unchecked((byte)r.Color))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckedWideningCastTranslates()
    {
        using TestDatabase db = SetupDatabase();

        long expected = db.Table<EnumNarrowingRow>().AsEnumerable()
            .Select(r => checked((long)r.Number))
            .First();

        Assert.Equal(42, expected);

        long actual = db.Table<EnumNarrowingRow>()
            .Select(r => checked((long)r.Number))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckedInRangeNarrowingCastTranslates()
    {
        using TestDatabase db = SetupDatabase();

        byte expected = db.Table<EnumNarrowingRow>().AsEnumerable()
            .Select(r => checked((byte)r.Number))
            .First();

        Assert.Equal(42, expected);

        byte actual = db.Table<EnumNarrowingRow>()
            .Select(r => checked((byte)r.Number))
            .First();

        Assert.Equal(expected, actual);
    }
}
