using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DecimalRealToStringParityTests
{
    private static TestDatabase Seed(params decimal[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, DecimalValue = values[i] });
        }

        return db;
    }

    [Fact]
    public void DecimalToString_WholeNumber_ScaleZero_MatchesDotNet()
    {
        decimal[] values = [10m, 0m, 1m, 100m, -5m];
        using TestDatabase db = Seed(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DecimalToString_Fractional_MatchesDotNet()
    {
        decimal[] values = [10.5m, 0.1m, 1.25m, 99.99m];
        using TestDatabase db = Seed(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }
}

public class DecimalTextToStringParityTests
{
    private static TestDatabase Seed(params decimal[] values)
    {
        TestDatabase db = new(o => o.DecimalStorage = SQLite.Framework.Enums.DecimalStorageMode.Text);
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, DecimalValue = values[i] });
        }

        return db;
    }

    [Fact]
    public void DecimalToString_TextStorage_WholeNumber_MatchesDotNet()
    {
        decimal[] values = [10m, 0m, -5m];
        using TestDatabase db = Seed(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DecimalToString_TextStorage_KeepsScale()
    {
        decimal[] values = [10.50m, 1.0m];
        using TestDatabase db = Seed(values);

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.DecimalValue.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
