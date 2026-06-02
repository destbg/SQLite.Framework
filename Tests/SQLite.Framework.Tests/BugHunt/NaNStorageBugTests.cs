using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class NullableDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double? Value { get; set; }
}

internal sealed class NonNullableDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double Value { get; set; }
}

internal sealed class NonNullableFloatRow
{
    [Key]
    public int Id { get; set; }

    public float Value { get; set; }
}

public class NaNStorageBugTests
{
    [Fact]
    public void NullableDoubleNaN_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NullableDoubleRow>().Schema.CreateTable();
        db.Table<NullableDoubleRow>().Add(new NullableDoubleRow { Id = 1, Value = double.NaN });

        NullableDoubleRow[] seed = [new NullableDoubleRow { Id = 1, Value = double.NaN }];
        double? expected = seed.First().Value;
        double? actual = db.Table<NullableDoubleRow>().First().Value;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonNullableDoubleNaN_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NonNullableDoubleRow>().Schema.CreateTable();
        db.Table<NonNullableDoubleRow>().Add(new NonNullableDoubleRow { Id = 1, Value = double.NaN });

        NonNullableDoubleRow[] seed = [new NonNullableDoubleRow { Id = 1, Value = double.NaN }];
        double expected = seed.First().Value;
        double actual = db.Table<NonNullableDoubleRow>().First().Value;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonNullableFloatNaN_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NonNullableFloatRow>().Schema.CreateTable();
        db.Table<NonNullableFloatRow>().Add(new NonNullableFloatRow { Id = 1, Value = float.NaN });

        NonNullableFloatRow[] seed = [new NonNullableFloatRow { Id = 1, Value = float.NaN }];
        float expected = seed.First().Value;
        float actual = db.Table<NonNullableFloatRow>().First().Value;

        Assert.Equal(expected, actual);
    }
}
