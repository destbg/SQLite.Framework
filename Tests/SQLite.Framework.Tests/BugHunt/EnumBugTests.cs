using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal enum UlongEnum : ulong
{
    A = 1,
}

internal sealed class UlongEnumRow
{
    [Key]
    public int Id { get; set; }

    public UlongEnum Value { get; set; }

    public string Code { get; set; } = "";
}

public class EnumBugTests
{
    [Fact]
    public void EnumToString_UlongUndefinedValue_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 1, Value = (UlongEnum)9999999999999999999UL });

        string expected = ((UlongEnum)9999999999999999999UL).ToString();
        string actual = db.Table<UlongEnumRow>().Select(r => r.Value.ToString()).First();

        Assert.Equal(expected, actual);
    }
}
