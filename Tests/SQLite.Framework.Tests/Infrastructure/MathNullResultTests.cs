using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathNullResultTests
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
    public void Sqrt_NegativeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(-1.0);

        Assert.True(double.IsNaN(Math.Sqrt(-1.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Sqrt(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_NegativeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(-1.0);

        Assert.True(double.IsNaN(Math.Log(-1.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log_ZeroInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(0.0);

        Assert.True(double.IsNegativeInfinity(Math.Log(0.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log2_ZeroInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(0.0);

        Assert.True(double.IsNegativeInfinity(Math.Log2(0.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log2(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log10_ZeroInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(0.0);

        Assert.True(double.IsNegativeInfinity(Math.Log10(0.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log10(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Asin_OutOfRangeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(2.0);

        Assert.True(double.IsNaN(Math.Asin(2.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Asin(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Acos_OutOfRangeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(2.0);

        Assert.True(double.IsNaN(Math.Acos(2.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Acos(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log2_NegativeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(-1.0);

        Assert.True(double.IsNaN(Math.Log2(-1.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log2(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Log10_NegativeInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(-1.0);

        Assert.True(double.IsNaN(Math.Log10(-1.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Log10(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Acosh_BelowOneInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(0.5);

        Assert.True(double.IsNaN(Math.Acosh(0.5)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Acosh(n.DoubleValue)).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Atanh_AboveOneInput_ReadsAsNull()
    {
        using TestDatabase db = SeedDouble(2.0);

        Assert.True(double.IsNaN(Math.Atanh(2.0)));
        double? actual = db.Table<NumericType>().Select(n => (double?)Math.Atanh(n.DoubleValue)).First();

        Assert.Null(actual);
    }
}
