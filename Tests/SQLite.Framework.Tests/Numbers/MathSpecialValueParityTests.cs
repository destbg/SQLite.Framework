using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathSpecialValueParityTests
{
    private static TestDatabase SeedDouble(double value)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType
        {
            Id = 1,
            DoubleValue = value,
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
        return db;
    }

    [Fact]
    public void Log_PositiveInfinityInput_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(double.PositiveInfinity);

        double? oracle = Math.Log(double.PositiveInfinity);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Atanh_OneInput_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(1.0);

        double? oracle = Math.Atanh(1.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Atanh(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Atanh_NegativeOneInput_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(-1.0);

        double? oracle = Math.Atanh(-1.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Atanh(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Exp_LargeInput_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(1000.0);

        double? oracle = Math.Exp(1000.0);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Exp(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Acosh_PositiveInfinityInput_MatchesDotNet()
    {
        using TestDatabase db = SeedDouble(double.PositiveInfinity);

        double? oracle = Math.Acosh(double.PositiveInfinity);
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Acosh(n.DoubleValue)).First();

        Assert.Equal(oracle, actual);
    }
}
