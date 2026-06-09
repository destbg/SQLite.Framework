using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullableUlongRows")]
file sealed class NullableUlongRow
{
    [Key]
    public int Id { get; set; }

    public ulong? Value { get; set; }
}

public class NullableUlongToStringSignedTests
{
    private static readonly (int Id, ulong? Value)[] Seed =
    [
        (1, 5UL),
        (2, 18446744073709551613UL),
        (3, 9223372036854775808UL),
        (4, null),
        (5, ulong.MaxValue),
    ];

    [Fact]
    public void NullableUlong_ToString_AboveLongMax_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableUlongRow>().Schema.CreateTable();
        foreach ((int id, ulong? value) in Seed)
        {
            db.Table<NullableUlongRow>().Add(new NullableUlongRow { Id = id, Value = value });
        }

        List<string> expected = Seed.OrderBy(r => r.Id).Select(r => r.Value.ToString()).ToList();
        List<string> actual = db.Table<NullableUlongRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Value.ToString())
            .ToList();

        Assert.Equal(["5", "18446744073709551613", "9223372036854775808", "", "18446744073709551615"], expected);
        Assert.Equal(expected, actual);
    }
}
