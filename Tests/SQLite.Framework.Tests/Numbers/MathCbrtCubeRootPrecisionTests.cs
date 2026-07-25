using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathCbrtCubeRootPrecisionTests
{
    private static TestDatabase SeedDouble(params double[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType
            {
                Id = i + 1,
                DoubleValue = values[i],
                IntValue = 0,
                LongValue = 0,
                ShortValue = 0,
                ByteValue = 0,
                SByteValue = 0,
                UIntValue = 0,
                ULongValue = 0,
                UShortValue = 0,
                FloatValue = 0,
                DecimalValue = 0,
                CharValue = 'A',
            });
        }
        return db;
    }

    [Fact]
    public void CbrtOfCubeOfFour_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(64.0);

        double oracle = Math.Cbrt(64.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfCubeOfFive_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(125.0);

        double oracle = Math.Cbrt(125.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfCubeOfSix_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(216.0);

        double oracle = Math.Cbrt(216.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfCubeOfTen_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(1000.0);

        double oracle = Math.Cbrt(1000.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfCubeOfNegativeFive_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(-125.0);

        double oracle = Math.Cbrt(-125.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfCubeOfNegativeTen_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(-1000.0);

        double oracle = Math.Cbrt(-1000.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfNonCubeValue_MatchesDotNetToTwelveDigits()
    {
        using TestDatabase db = SeedDouble(123.456);

        double oracle = Math.Cbrt(123.456);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual, 12);
    }

    [Fact]
    public void CbrtOfZero_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(0.0);

        double oracle = Math.Cbrt(0.0);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CbrtOfPositiveInfinity_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(double.PositiveInfinity);

        double oracle = Math.Cbrt(double.PositiveInfinity);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CbrtOfNegativeInfinity_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(double.NegativeInfinity);

        double oracle = Math.Cbrt(double.NegativeInfinity);
        double actual = db.Table<NumericType>().Select(n => Math.Cbrt(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }
}
