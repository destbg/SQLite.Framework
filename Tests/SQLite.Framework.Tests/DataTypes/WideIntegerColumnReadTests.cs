using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24hWideRows")]
public class H24hWideRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

[Table("H24hFlagRows")]
public class H24hFlagRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }
}

public class WideIntegerColumnReadTests
{
    [Fact]
    public void ReadingAStoredValueAboveIntRangeIntoAnIntDoesNotTruncate()
    {
        using TestDatabase db = new();
        db.Table<H24hWideRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"H24hWideRows\" (\"Id\", \"Amount\") VALUES (1, 5000000000)");

        Exception? failure = Record.Exception(() => db.Table<H24hWideRow>().ToList());

        Assert.NotNull(failure);
    }

    [Fact]
    public void ReadingAStoredValueInsideIntRangeIntoAnIntKeepsTheValue()
    {
        using TestDatabase db = new();
        db.Table<H24hWideRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"H24hWideRows\" (\"Id\", \"Amount\") VALUES (1, 2000000000)");

        Assert.Equal(2000000000, db.Table<H24hWideRow>().Single().Amount);
    }

    [Fact]
    public void ReadingAStoredValueWithOnlyHighBitsSetIntoABoolReadsAsTrue()
    {
        using TestDatabase db = new();
        db.Table<H24hFlagRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"H24hFlagRows\" (\"Id\", \"Flag\") VALUES (1, 4294967296)");

        Assert.True(db.Table<H24hFlagRow>().Single().Flag);
    }
}
