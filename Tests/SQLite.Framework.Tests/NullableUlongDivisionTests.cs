using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullableUlongMathRows")]
file sealed class NullableUlongMathRow
{
    [Key]
    public int Id { get; set; }

    public ulong? Value { get; set; }
}

public class NullableUlongDivisionTests
{
    [Fact]
    public void DivideNullableUlongAboveLongMaxMatchesDotNet()
    {
        ulong? value = 1UL << 63;

        using TestDatabase db = new();
        db.Table<NullableUlongMathRow>().Schema.CreateTable();
        db.Table<NullableUlongMathRow>().Add(new NullableUlongMathRow { Id = 1, Value = value });

        ulong? oracle = value / 2UL;
        Assert.Equal(4611686018427387904UL, oracle);

        ulong? actual = db.Table<NullableUlongMathRow>()
            .Select(r => r.Value / 2UL)
            .First();

        Assert.Equal(oracle, actual);
    }
}
