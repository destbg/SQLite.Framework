using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UlongToStringRows")]
file sealed class UlongToStringRow
{
    [Key]
    public int Id { get; set; }

    public ulong Value { get; set; }
}

public class UlongToStringTests
{
    private static readonly ulong[] Values =
    {
        0UL,
        5UL,
        (ulong)long.MaxValue,
        (ulong)long.MaxValue + 1UL,
        ulong.MaxValue,
    };

    [Fact]
    public void UlongToStringMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<UlongToStringRow>().Schema.CreateTable();
        for (int i = 0; i < Values.Length; i++)
        {
            db.Table<UlongToStringRow>().Add(new UlongToStringRow { Id = i + 1, Value = Values[i] });
        }

        List<string> actual = db.Table<UlongToStringRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Value.ToString())
            .ToList();
        List<string> expected = Values.Select(v => v.ToString()).ToList();

        Assert.Equal(expected, actual);
    }
}
