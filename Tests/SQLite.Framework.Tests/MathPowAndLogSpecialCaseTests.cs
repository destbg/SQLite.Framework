using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathPowAndLogSpecialCaseTests
{
    private static TestDatabase SeedTwo(double a, double b)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType
        {
            Id = 1,
            DoubleValue = a,
            FloatValue = (float)b,
            IntValue = 0,
            LongValue = 0,
            ShortValue = 0,
            ByteValue = 0,
            SByteValue = 0,
            UIntValue = 0,
            ULongValue = 0,
            UShortValue = 0,
            DecimalValue = 0,
            CharValue = 'A',
        });
        return db;
    }

    [Fact]
    public void Pow_NegativeBaseWithFractionalExponent_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(-2.0, 0.5);

        Assert.True(double.IsNaN(Math.Pow(-2.0, 0.5)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Pow(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Pow_ZeroBaseNegativeExponent_MatchesDotNet()
    {
        using TestDatabase db = SeedTwo(0.0, -1.0);

        double? oracle = Math.Pow(0.0, -1.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Pow(n.DoubleValue, n.FloatValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Log_WithBaseOne_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(8.0, 1.0);

        Assert.True(double.IsNaN(Math.Log(8.0, 1.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_WithBaseZero_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(8.0, 0.0);

        Assert.True(double.IsNaN(Math.Log(8.0, 0.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_WithNegativeBase_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(8.0, -2.0);

        Assert.True(double.IsNaN(Math.Log(8.0, -2.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_ZeroValueWithBaseAboveOne_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(0.0, 2.0);

        Assert.True(double.IsNegativeInfinity(Math.Log(0.0, 2.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_ZeroValueWithBaseBelowOne_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(0.0, 0.5);

        Assert.True(double.IsPositiveInfinity(Math.Log(0.0, 0.5)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_OneValueWithBaseZero_ReadsAsNull()
    {
        using TestDatabase db = SeedTwo(1.0, 0.0);

        Assert.Equal(0.0, Math.Log(1.0, 0.0));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_OneValueWithNormalBase_MatchesDotNet()
    {
        using TestDatabase db = SeedTwo(1.0, 2.0);

        double? oracle = Math.Log(1.0, 2.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Log_NormalValueWithNormalBase_MatchesDotNet()
    {
        using TestDatabase db = SeedTwo(8.0, 2.0);

        double oracle = Math.Log(8.0, 2.0);
        double actual = db.Table<NumericType>().Select(n => Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Equal(oracle, actual, 10);
    }

    [Fact]
    public void Log_PositiveInfinityValueWithNormalBase_MatchesDotNet()
    {
        using TestDatabase db = SeedTwo(double.PositiveInfinity, 2.0);

        double? oracle = Math.Log(double.PositiveInfinity, 2.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue, n.FloatValue)).First();

        Assert.Equal(oracle, actual);
    }
}
