using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatToUnsignedCastParityTests
{
    private static NumericType Make(
        int id,
        int iv = 0,
        long lv = 0,
        short sv = 0,
        byte bv = 0,
        sbyte sbv = 0,
        uint uiv = 0,
        ulong ulv = 0,
        ushort usv = 0,
        double dv = 0,
        float fv = 0,
        decimal decv = 0,
        char cv = 'A')
    {
        return new NumericType
        {
            Id = id,
            IntValue = iv,
            LongValue = lv,
            ShortValue = sv,
            ByteValue = bv,
            SByteValue = sbv,
            UIntValue = uiv,
            ULongValue = ulv,
            UShortValue = usv,
            DoubleValue = dv,
            FloatValue = fv,
            DecimalValue = decv,
            CharValue = cv
        };
    }

    private static TestDatabase Seed(IEnumerable<NumericType> rows, Action<SQLiteOptionsBuilder>? cfg = null)
    {
        TestDatabase db = cfg == null ? new() : new(cfg);
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(rows.ToList());
        return db;
    }

    [Fact]
    public void NegativeDoubleToUintWraps()
    {
        List<NumericType> data = new() { Make(1, dv: -1.0) };
        using TestDatabase db = Seed(data);
        var expected = data.Select(r => unchecked((uint)(long)r.DoubleValue)).ToList();
        var actual = db.Table<NumericType>().Select(r => (uint)r.DoubleValue).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegativeFloatToUintWraps()
    {
        List<NumericType> data = new() { Make(1, fv: -1f) };
        using TestDatabase db = Seed(data);
        var expected = data.Select(r => unchecked((uint)(long)r.FloatValue)).ToList();
        var actual = db.Table<NumericType>().Select(r => (uint)r.FloatValue).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegativeDoubleToUlongWraps()
    {
        List<NumericType> data = new() { Make(1, dv: -3.0) };
        using TestDatabase db = Seed(data);
        var expected = data.Select(r => unchecked((ulong)(long)r.DoubleValue)).ToList();
        var actual = db.Table<NumericType>().Select(r => (ulong)r.DoubleValue).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleAboveUintRangeWraps()
    {
        List<NumericType> data = new() { Make(1, dv: 1e10) };
        using TestDatabase db = Seed(data);
        var expected = data.Select(r => unchecked((uint)(long)r.DoubleValue)).ToList();
        var actual = db.Table<NumericType>().Select(r => (uint)r.DoubleValue).ToList();
        Assert.Equal(expected, actual);
    }
}
