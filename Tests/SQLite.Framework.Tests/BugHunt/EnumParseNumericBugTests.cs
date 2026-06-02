using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

file enum HuntSmallEnum
{
    A = 1,
    B = 2
}

[Table("HuntEnumParseRows")]
file sealed class HuntEnumParseRow
{
    [Key]
    public int Id { get; set; }

    public HuntSmallEnum Value { get; set; }

    public string Code { get; set; } = "";
}

public class EnumParseNumericBugTests
{
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
