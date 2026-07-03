using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongParseCodeRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public class UlongParseAboveSignedRangeTests
{
    [Fact]
    public void UlongParseMaxValueText()
    {
        using TestDatabase db = new();
        db.Table<UlongParseCodeRow>().Schema.CreateTable();
        db.Table<UlongParseCodeRow>().Add(new UlongParseCodeRow { Id = 1, Code = "18446744073709551615" });

        List<UlongParseCodeRow> rows = [new() { Id = 1, Code = "18446744073709551615" }];
        ulong expected = rows.Select(r => ulong.Parse(r.Code)).First();
        Assert.Equal(18446744073709551615UL, expected);

        ulong actual = db.Table<UlongParseCodeRow>().Select(r => ulong.Parse(r.Code)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UlongParseJustAboveSignedRange()
    {
        using TestDatabase db = new();
        db.Table<UlongParseCodeRow>().Schema.CreateTable();
        db.Table<UlongParseCodeRow>().Add(new UlongParseCodeRow { Id = 1, Code = "9223372036854775808" });

        List<UlongParseCodeRow> rows = [new() { Id = 1, Code = "9223372036854775808" }];
        ulong expected = rows.Select(r => ulong.Parse(r.Code)).First();
        Assert.Equal(9223372036854775808UL, expected);

        ulong actual = db.Table<UlongParseCodeRow>().Select(r => ulong.Parse(r.Code)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UintParseMaxValueStaysCorrect()
    {
        using TestDatabase db = new();
        db.Table<UlongParseCodeRow>().Schema.CreateTable();
        db.Table<UlongParseCodeRow>().Add(new UlongParseCodeRow { Id = 1, Code = "4294967295" });

        List<UlongParseCodeRow> rows = [new() { Id = 1, Code = "4294967295" }];
        uint expected = rows.Select(r => uint.Parse(r.Code)).First();
        Assert.Equal(4294967295U, expected);

        uint actual = db.Table<UlongParseCodeRow>().Select(r => uint.Parse(r.Code)).First();
        Assert.Equal(expected, actual);
    }
}
