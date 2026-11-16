using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DoubleToDecimalOverflowParityTests
{
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(0.1, "0.1")]
    [InlineData(-0.1, "-0.1")]
    [InlineData(9.99, "9.99")]
    [InlineData(1.23, "1.23")]
    [InlineData(10.123456789, "10.123456789")]
    [InlineData(5e-29, "0")]
    [InlineData(1e-28, "0.0000000000000000000000000001")]
    [InlineData(double.Epsilon, "0")]
    [InlineData(1e14, "100000000000000")]
    [InlineData(1e15, "1000000000000000")]
    [InlineData(1e16, "10000000000000000")]
    [InlineData(-1e16, "-10000000000000000")]
    [InlineData(1e-14, "0.00000000000001")]
    public void NumericReadPreservesDecimalValues(double value, string expected)
    {
        using TestDatabase db = new();
        decimal actual = db.ExecuteScalar<decimal>("SELECT @value", new SQLiteParameter { Name = "@value", Value = value });

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), actual);
    }

#if NET11_0_OR_GREATER
    [Theory]
    [InlineData(0.23333333333333334)]
    [InlineData(5.0000000000000004e-29)]
    [InlineData(102711978881.95149)]
    [InlineData(-5783558579060.975)]
    [InlineData(2.349238650604645e17)]
    [InlineData(7.922816251426433e28)]
    public void NumericReadMatchesSqliteTextWithoutChangingTheDouble(double value)
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("SELECT @value, CAST(@value AS TEXT)",
            [new SQLiteParameter { Name = "@value", Value = value }]).ExecuteReader();
        Assert.True(reader.Read());
        decimal expected = decimal.Parse(reader.GetString(1)!, NumberStyles.Float, CultureInfo.InvariantCulture);

        Assert.Equal(expected, reader.GetDecimalValue(0));
        Assert.Equal(value, reader.GetDouble(0));
        Assert.Equal(expected, reader.GetDecimalValue(0));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(9007199254740993L)]
    [InlineData(long.MaxValue)]
    public void IntegerReadPreservesDigitsBeyondDoublePrecision(long value)
    {
        using TestDatabase db = new();
        decimal actual = db.ExecuteScalar<decimal>("SELECT @value", new SQLiteParameter { Name = "@value", Value = value });

        Assert.Equal((decimal)value, actual);
    }
#endif

    [Fact]
    public void DoubleAboveDecimalRangeToDecimal_ClampsToMaxValue()
    {
        using TestDatabase db = new();
        db.Table<DoubleDecimalRow>().Schema.CreateTable();
        db.Table<DoubleDecimalRow>().Add(new DoubleDecimalRow { Id = 1, DoubleValue = 1e30 });
        db.Table<DoubleDecimalRow>().Add(new DoubleDecimalRow { Id = 2, DoubleValue = -1e30 });

        List<decimal> actual = db.Table<DoubleDecimalRow>().OrderBy(r => r.Id).Select(r => (decimal)r.DoubleValue).ToList();

        Assert.Equal(new List<decimal> { decimal.MaxValue, decimal.MinValue }, actual);
    }

    [Fact]
    public void DoubleInDecimalRangeToDecimal_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<DoubleDecimalRow>().Schema.CreateTable();
        db.Table<DoubleDecimalRow>().Add(new DoubleDecimalRow { Id = 1, DoubleValue = 12345.5 });

        List<(int Id, double DoubleValue)> seed = new() { (1, 12345.5) };

        List<decimal> expected = seed.Select(r => (decimal)r.DoubleValue).ToList();
        List<decimal> actual = db.Table<DoubleDecimalRow>().OrderBy(r => r.Id).Select(r => (decimal)r.DoubleValue).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("DoubleDecimalRows")]
public class DoubleDecimalRow
{
    [Key]
    public int Id { get; set; }

    public double DoubleValue { get; set; }
}
