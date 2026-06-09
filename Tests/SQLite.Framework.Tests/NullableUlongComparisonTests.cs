using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullableUlongCmpRows")]
file sealed class NullableUlongCmpRow
{
    [Key]
    public int Id { get; set; }

    public ulong? Value { get; set; }
}

public class NullableUlongComparisonTests
{
    [Fact]
    public void GreaterThanOnNullableUlongAboveLongMaxMatchesDotNet()
    {
        (int id, ulong? val)[] rows =
        {
            (1, 1UL << 63),
            (2, 5UL),
            (3, ulong.MaxValue),
        };

        using TestDatabase db = new();
        db.Table<NullableUlongCmpRow>().Schema.CreateTable();
        foreach ((int id, ulong? val) in rows)
        {
            db.Table<NullableUlongCmpRow>().Add(new NullableUlongCmpRow { Id = id, Value = val });
        }

        List<int> oracle = rows.Where(r => r.val > 100UL).Select(r => r.id).OrderBy(i => i).ToList();
        Assert.Equal(new List<int> { 1, 3 }, oracle);

        List<int> actual = db.Table<NullableUlongCmpRow>()
            .Where(x => x.Value > 100UL)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
