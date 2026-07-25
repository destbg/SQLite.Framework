using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UlongMathRows")]
file sealed class UlongMathRow
{
    [Key]
    public int Id { get; set; }

    public ulong Value { get; set; }
}

public class UlongDivisionTests
{
    [Theory]
    [InlineData(ulong.MaxValue, 2UL)]
    [InlineData(ulong.MaxValue, 10UL)]
    [InlineData(ulong.MaxValue, 1UL)]
    [InlineData(ulong.MaxValue, ulong.MaxValue)]
    [InlineData(ulong.MaxValue, 9223372036854775808UL)]
    [InlineData(9223372036854775808UL, 2UL)]
    [InlineData(9223372036854775808UL, ulong.MaxValue)]
    [InlineData(9223372036854775808UL, 3UL)]
    [InlineData(0UL, 5UL)]
    [InlineData(100UL, 7UL)]
    [InlineData(9223372036854775807UL, 3UL)]
    [InlineData(12345678901234567890UL, 1000000007UL)]
    public void UlongDivideAndModuloMatchDotNet(ulong value, ulong divisor)
    {
        using TestDatabase db = new();
        db.Table<UlongMathRow>().Schema.CreateTable();
        db.Table<UlongMathRow>().Add(new UlongMathRow { Id = 1, Value = value });

        ulong actualDiv = db.Table<UlongMathRow>().Select(r => r.Value / divisor).First();
        ulong actualMod = db.Table<UlongMathRow>().Select(r => r.Value % divisor).First();

        Assert.Equal(value / divisor, actualDiv);
        Assert.Equal(value % divisor, actualMod);
    }
}
