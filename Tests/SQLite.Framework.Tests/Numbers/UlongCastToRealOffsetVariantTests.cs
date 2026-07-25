using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum BigUlongMarker : ulong
{
    Small = 3,
    Huge = 18446744073709551615UL
}

internal sealed class UlongCastRow
{
    [Key]
    public int Id { get; set; }

    public ulong Plain { get; set; }

    public ulong? Optional { get; set; }

    public BigUlongMarker Marker { get; set; }
}

public class UlongCastToRealOffsetVariantTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<UlongCastRow>().Schema.CreateTable();
        db.Table<UlongCastRow>().Add(new UlongCastRow
        {
            Id = 1,
            Plain = 18446744073709551615UL,
            Optional = 18446744073709551610UL,
            Marker = BigUlongMarker.Huge
        });
        return db;
    }

    [Fact]
    public void NullableUlongToDoubleAppliesUnsignedOffset()
    {
        using TestDatabase db = SetupDatabase();

        double? expected = db.Table<UlongCastRow>().AsEnumerable()
            .Select(r => (double?)r.Optional)
            .First();

        Assert.Equal(1.8446744073709552E+19, expected);

        double? actual = db.Table<UlongCastRow>()
            .Select(r => (double?)r.Optional)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UlongBackedEnumToDoubleAppliesUnsignedOffset()
    {
        using TestDatabase db = SetupDatabase();

        double expected = db.Table<UlongCastRow>().AsEnumerable()
            .Select(r => (double)r.Marker)
            .First();

        Assert.Equal(1.8446744073709552E+19, expected);

        double actual = db.Table<UlongCastRow>()
            .Select(r => (double)r.Marker)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UlongToDecimalAppliesUnsignedOffset()
    {
        using TestDatabase db = SetupDatabase();

        decimal expected = db.Table<UlongCastRow>().AsEnumerable()
            .Select(r => (decimal)r.Plain)
            .First();

        Assert.Equal(18446744073709551615m, expected);

        decimal actual = db.Table<UlongCastRow>()
            .Select(r => (decimal)r.Plain)
            .First();

        double relativeError = Math.Abs((double)actual - (double)expected) / (double)expected;
        Assert.True(relativeError < 1e-12);
    }
}
