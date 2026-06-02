using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

file enum HuntSmallEnum
{
    A = 1,
    B = 2
}

[Table("HuntUlongRows")]
file sealed class HuntUlongRow
{
    [Key]
    public int Id { get; set; }

    public ulong Value { get; set; }
}

[Table("HuntEnumParseRows")]
file sealed class HuntEnumParseRow
{
    [Key]
    public int Id { get; set; }

    public HuntSmallEnum Value { get; set; }

    public string Code { get; set; } = "";
}

public class UnsignedNumericAndEnumParseBugTests
{
    [Fact]
    public void UlongDivideAboveLongMaxMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<HuntUlongRow>().Schema.CreateTable();
        db.Table<HuntUlongRow>().Add(new HuntUlongRow { Id = 1, Value = ulong.MaxValue });

        ulong actual = db.Table<HuntUlongRow>().Select(r => r.Value / 2UL).First();
        ulong expected = ulong.MaxValue / 2UL;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UlongModuloAboveLongMaxMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<HuntUlongRow>().Schema.CreateTable();
        db.Table<HuntUlongRow>().Add(new HuntUlongRow { Id = 1, Value = ulong.MaxValue });

        ulong actual = db.Table<HuntUlongRow>().Select(r => r.Value % 10UL).First();
        ulong expected = ulong.MaxValue % 10UL;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParseNumericStringMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<HuntEnumParseRow>().Schema.CreateTable();
        db.Table<HuntEnumParseRow>().Add(new HuntEnumParseRow { Id = 1, Value = HuntSmallEnum.A, Code = "1" });

        List<int> actual = db.Table<HuntEnumParseRow>()
            .Where(r => r.Value == Enum.Parse<HuntSmallEnum>(r.Code))
            .Select(r => r.Id)
            .ToList();

        HuntEnumParseRow[] rows = { new() { Id = 1, Value = HuntSmallEnum.A, Code = "1" } };
        List<int> expected = rows
            .Where(r => r.Value == Enum.Parse<HuntSmallEnum>(r.Code))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
