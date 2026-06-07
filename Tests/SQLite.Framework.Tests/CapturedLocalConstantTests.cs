using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CapturedLocalConstantTests
{
    private static TestDatabase SeededNums(int[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = values[i] });
        }

        return db;
    }

    [Fact]
    public void CapturedListCount_MatchesDotNet()
    {
        int[] data = [1, 2, 3, 4, 5];
        using TestDatabase db = SeededNums(data);
        List<int> bound = [10, 20, 30];

        List<int> expected = data.Where(v => v <= bound.Count).OrderBy(v => v).ToList();
        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue <= bound.Count).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayLength_MatchesDotNet()
    {
        int[] data = [1, 2, 3, 4, 5];
        using TestDatabase db = SeededNums(data);
        int[] bound = [10, 20, 30];

        List<int> expected = data.Where(v => v <= bound.Length).OrderBy(v => v).ToList();
        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue <= bound.Length).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayElement_MatchesDotNet()
    {
        int[] data = [1, 2, 3, 4, 5];
        using TestDatabase db = SeededNums(data);
        int[] wanted = [3, 20, 30];

        List<int> expected = data.Where(v => v == wanted[0]).OrderBy(v => v).ToList();
        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue == wanted[0]).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedIntLocal_MatchesDotNet()
    {
        int[] data = [-3, -1, 0, 3, 5];
        using TestDatabase db = SeededNums(data);
        int threshold = 3;

        List<int> expected = data.Where(v => v == -threshold).OrderBy(v => v).ToList();
        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue == -threshold).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplementIntLocal_MatchesDotNet()
    {
        int[] data = [-4, -3, 2, 3, 5];
        using TestDatabase db = SeededNums(data);
        int mask = 2;

        List<int> expected = data.Where(v => v == ~mask).OrderBy(v => v).ToList();
        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue == ~mask).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedLongLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        long[] data = [-3, 0, 3, 7];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, LongValue = data[i] });
        }
        long threshold = 3;

        List<long> expected = data.Where(v => v == -threshold).OrderBy(v => v).ToList();
        List<long> actual = db.Table<NumericType>().Where(n => n.LongValue == -threshold).Select(n => n.LongValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplementLongLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        long[] data = [-4, -3, 2, 5];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, LongValue = data[i] });
        }
        long mask = 2;

        List<long> expected = data.Where(v => v == ~mask).OrderBy(v => v).ToList();
        List<long> actual = db.Table<NumericType>().Where(n => n.LongValue == ~mask).Select(n => n.LongValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplementUIntLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        uint[] data = [0, 2, 4294967293];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, UIntValue = data[i] });
        }
        uint mask = 2;

        List<uint> expected = data.Where(v => v == ~mask).OrderBy(v => v).ToList();
        List<uint> actual = db.Table<NumericType>().Where(n => n.UIntValue == ~mask).Select(n => n.UIntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplementULongLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        ulong[] data = [0, 2, 18446744073709551613];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, ULongValue = data[i] });
        }
        ulong mask = 2;

        List<ulong> expected = data.Where(v => v == ~mask).OrderBy(v => v).ToList();
        List<ulong> actual = db.Table<NumericType>().Where(n => n.ULongValue == ~mask).Select(n => n.ULongValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedFloatLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        float[] data = [-2.5f, 0f, 2.5f, 4f];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, FloatValue = data[i] });
        }
        float threshold = 2.5f;

        List<float> expected = data.Where(v => v == -threshold).OrderBy(v => v).ToList();
        List<float> actual = db.Table<NumericType>().Where(n => n.FloatValue == -threshold).Select(n => n.FloatValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedDoubleLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        double[] data = [-1.5, 0, 1.5, 3];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, DoubleValue = data[i] });
        }
        double threshold = 1.5;

        List<double> expected = data.Where(v => v == -threshold).OrderBy(v => v).ToList();
        List<double> actual = db.Table<NumericType>().Where(n => n.DoubleValue == -threshold).Select(n => n.DoubleValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedDecimalLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        decimal[] data = [-1.5m, 0m, 1.5m, 3m];
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, DecimalValue = data[i] });
        }
        decimal threshold = 1.5m;

        List<decimal> expected = data.Where(v => v == -threshold).OrderBy(v => v).ToList();
        List<decimal> actual = db.Table<NumericType>().Where(n => n.DecimalValue == -threshold).Select(n => n.DecimalValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedLogicalNotLocal_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableBoolEntity>().Schema.CreateTable();
        bool?[] flags = [true, false, null];
        for (int i = 0; i < flags.Length; i++)
        {
            db.Table<NullableBoolEntity>().Add(new NullableBoolEntity { Id = i + 1, Flag = flags[i] });
        }

        bool wanted = false;

        List<int> expected = Enumerable.Range(1, flags.Length).Where(i => flags[i - 1] == !wanted).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableBoolEntity>().Where(x => x.Flag == !wanted).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedNegatedUnsupportedLocal_Throws()
    {
        using TestDatabase db = SeededNums([1, 2, 3]);
        Half threshold = (Half)2;

        Assert.Throws<NotSupportedException>(() =>
            db.Table<NumericType>().Where(n => n.IntValue == (int)(-threshold)).ToList());
    }
}
