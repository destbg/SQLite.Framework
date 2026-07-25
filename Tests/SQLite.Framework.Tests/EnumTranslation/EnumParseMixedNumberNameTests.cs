using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
internal enum ParsePerm
{
    None = 0,
    Read = 1,
    Write = 2,
    Exec = 4,
}

internal sealed class ParsePermRow
{
    [Key]
    public int Id { get; set; }

    public string Value { get; set; } = "";
}

public class EnumParseMixedNumberNameTests
{
    [Theory]
    [InlineData("1,2", 1)]
    [InlineData("2,Read", 3)]
    [InlineData("Write,1", 2)]
    [InlineData("2extra", 2)]
    public void ParseMixedNumberAndNameReturnsPartialValue(string value, int expected)
    {
        using TestDatabase db = new();
        db.Table<ParsePermRow>().Schema.CreateTable();
        db.Table<ParsePermRow>().Add(new ParsePermRow { Id = 1, Value = value });

        Assert.Throws<ArgumentException>(() => Enum.Parse<ParsePerm>(value));

        int actual = db.Table<ParsePermRow>().Select(x => (int)Enum.Parse<ParsePerm>(x.Value)).Single();

        Assert.Equal(expected, actual);
    }
}
