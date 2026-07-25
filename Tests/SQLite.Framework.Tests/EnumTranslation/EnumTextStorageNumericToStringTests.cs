using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum ToStringColor
{
    Red = 1,
    Green = 2,
    Blue = 4
}

internal sealed class EnumToStringRow
{
    [Key]
    public int Id { get; set; }

    public ToStringColor Color { get; set; }
}

public class EnumTextStorageNumericToStringTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<EnumToStringRow>().Schema.CreateTable();
        db.Table<EnumToStringRow>().Add(new EnumToStringRow { Id = 1, Color = ToStringColor.Green });
        return db;
    }

    [Fact]
    public void ToStringDecimalFormatReturnsNumber()
    {
        using TestDatabase db = SetupDatabase();

        string expected = ToStringColor.Green.ToString("D");

        Assert.Equal("2", expected);

        string actual = db.Table<EnumToStringRow>()
            .Select(r => r.Color.ToString("D"))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringHexFormatReturnsHex()
    {
        using TestDatabase db = SetupDatabase();

        string expected = ToStringColor.Green.ToString("X");

        Assert.Equal("00000002", expected);

        string actual = db.Table<EnumToStringRow>()
            .Select(r => r.Color.ToString("X"))
            .First();

        Assert.Equal(expected, actual);
    }
}
