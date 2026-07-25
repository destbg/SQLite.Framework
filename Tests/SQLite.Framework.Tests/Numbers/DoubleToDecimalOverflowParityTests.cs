using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DoubleToDecimalOverflowParityTests
{
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
