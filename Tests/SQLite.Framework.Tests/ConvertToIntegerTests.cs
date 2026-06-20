using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConvertToIntegerTests
{
    [Fact]
    public void ToInt32OfDoubleColumn_NonMidpoint_InSelectMatchesObjects()
    {
        using TestDatabase db = Seed(out List<ConvertIntRow> mem, 2.4, 2.6, -2.4, -2.6, 0.0, 9.49);

        List<int> expected = mem.OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.D)).ToList();
        List<int> actual = db.Table<ConvertIntRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.D)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToInt32OfDoubleColumn_NonMidpoint_InWhereMatchesObjects()
    {
        using TestDatabase db = Seed(out List<ConvertIntRow> mem, 2.4, 2.6, 3.4, -2.6, 9.49);

        List<int> expected = mem.Where(r => Convert.ToInt32(r.D) == 3).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ConvertIntRow>().Where(r => Convert.ToInt32(r.D) == 3).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToInt64OfDoubleColumn_NonMidpoint_InSelectMatchesObjects()
    {
        using TestDatabase db = Seed(out List<ConvertIntRow> mem, 100.4, 100.6, -100.4, 5000000000.6);

        List<long> expected = mem.OrderBy(r => r.Id).Select(r => Convert.ToInt64(r.D)).ToList();
        List<long> actual = db.Table<ConvertIntRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt64(r.D)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToInt32OfFloatColumn_NonMidpoint_InSelectMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<ConvertFloatRow>().Schema.CreateTable();
        float[] values = { 2.4f, 2.6f, -2.4f, 7.9f };
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<ConvertFloatRow>().Add(new ConvertFloatRow { Id = i + 1, F = values[i] });
        }

        List<int> expected = values.Select(v => Convert.ToInt32(v)).ToList();
        List<int> actual = db.Table<ConvertFloatRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.F)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToInt32OfDoubleColumn_Midpoint_RoundsAwayFromZero()
    {
        using TestDatabase db = Seed(out _, 2.5, 3.5, -2.5, -3.5, 0.5);

        List<int> actual = db.Table<ConvertIntRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.D)).ToList();

        Assert.Equal(new List<int> { 3, 4, -3, -4, 1 }, actual);
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR && !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void ToInt32OfIntegerColumn_RunsInMemory_MatchesObjects()
    {
        using TestDatabase db = Seed(out List<ConvertIntRow> mem, 1.0, 2.0, 3.0);

        List<int> expected = mem.OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.Id)).ToList();
        List<int> actual = db.Table<ConvertIntRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt32(r.Id)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToInt32OfUntranslatableDouble_RunsInMemory_MatchesObjects()
    {
        using TestDatabase db = Seed(out List<ConvertIntRow> mem, 2.4, 2.6, 3.4);

        List<int> expected = mem.OrderBy(r => r.Id).Select(r => Convert.ToInt32(InterceptorHelpers.IdentityDouble(r.D))).ToList();
        List<int> actual = db.Table<ConvertIntRow>().OrderBy(r => r.Id).Select(r => Convert.ToInt32(InterceptorHelpers.IdentityDouble(r.D))).ToList();

        Assert.Equal(expected, actual);
    }
#endif

    private static TestDatabase Seed(out List<ConvertIntRow> mem, params double[] values)
    {
        TestDatabase db = new();
        db.Table<ConvertIntRow>().Schema.CreateTable();
        mem = new List<ConvertIntRow>();
        for (int i = 0; i < values.Length; i++)
        {
            ConvertIntRow row = new() { Id = i + 1, D = values[i] };
            mem.Add(row);
            db.Table<ConvertIntRow>().Add(row);
        }

        return db;
    }
}

[Table("ConvertIntRows")]
public class ConvertIntRow
{
    [Key]
    public int Id { get; set; }

    public double D { get; set; }
}

[Table("ConvertFloatRows")]
public class ConvertFloatRow
{
    [Key]
    public int Id { get; set; }

    public float F { get; set; }
}
