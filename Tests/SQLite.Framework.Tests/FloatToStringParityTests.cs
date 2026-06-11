using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatToStringParityTests
{
    private static TestDatabase SeedDouble(params double[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, DoubleValue = values[i] });
        }

        return db;
    }

    private static TestDatabase SeedFloat(params float[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, FloatValue = values[i] });
        }

        return db;
    }

    [Fact]
    public void DoubleToString_WholeNumber_MatchesDotNet()
    {
        double[] values = [10.0, 0.0, 1.0, 100.0, -5.0];
        using TestDatabase db = SeedDouble(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DoubleValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleToString_FractionalNumber_MatchesDotNet()
    {
        double[] values = [10.5, 0.1, 1.25, 99.99, -5.75];
        using TestDatabase db = SeedDouble(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DoubleValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleToString_SmallScientificNotation_MatchesDotNet()
    {
        double[] values = [1.0e-10, 2.5e-5, 1.0e10, 1.5e20];
        using TestDatabase db = SeedDouble(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DoubleValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FloatToString_WholeNumber_MatchesDotNet()
    {
        float[] values = [10f, 0f, 1f, 100f, -5f];
        using TestDatabase db = SeedFloat(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.FloatValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableDoubleToString_WholeNumber_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableRealRow>().Schema.CreateTable();
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 1, DoubleValue = 10.0 });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 2, DoubleValue = null });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 3, DoubleValue = 5.5 });

        double?[] source = [10.0, null, 5.5];
        List<string> expected = source.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NullableRealRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.DoubleValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableFloatToString_WholeNumber_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableRealRow>().Schema.CreateTable();
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 1, FloatValue = 10f });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 2, FloatValue = null });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 3, FloatValue = 5.5f });

        float?[] source = [10f, null, 5.5f];
        List<string> expected = source.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NullableRealRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.FloatValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableDecimalToString_RealStorage_WholeNumber_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableRealRow>().Schema.CreateTable();
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 1, DecimalValue = 10m });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 2, DecimalValue = null });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 3, DecimalValue = 5.5m });

        decimal?[] source = [10m, null, 5.5m];
        List<string> expected = source.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NullableRealRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableDecimalToString_TextStorage_MatchesDotNet()
    {
        using TestDatabase db = new(o => o.DecimalStorage = SQLite.Framework.Enums.DecimalStorageMode.Text);
        db.Table<NullableRealRow>().Schema.CreateTable();
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 1, DecimalValue = 10m });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 2, DecimalValue = null });
        db.Table<NullableRealRow>().Add(new NullableRealRow { Id = 3, DecimalValue = 10.50m });

        decimal?[] source = [10m, null, 10.50m];
        List<string> expected = source.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NullableRealRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }
}

file sealed class NullableRealRow
{
    [Key]
    public int Id { get; set; }

    public double? DoubleValue { get; set; }

    public float? FloatValue { get; set; }

    public decimal? DecimalValue { get; set; }
}
