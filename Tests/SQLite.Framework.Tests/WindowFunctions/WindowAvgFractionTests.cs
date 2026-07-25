using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class WinAvgRow
{
    [Key]
    public int Id { get; set; }
    public int Grp { get; set; }
    public int IntAmt { get; set; }
    public long LongAmt { get; set; }
    public int? NullAmt { get; set; }
    public double DblAmt { get; set; }
}

public class WindowAvgFractionTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<WinAvgRow>().Schema.CreateTable();
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 1, Grp = 1, IntAmt = 2, LongAmt = 2, NullAmt = 2, DblAmt = 2 });
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 2, Grp = 1, IntAmt = 3, LongAmt = 3, NullAmt = null, DblAmt = 3 });
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 3, Grp = 1, IntAmt = 4, LongAmt = 4, NullAmt = 4, DblAmt = 4 });
        return db;
    }

    [Fact]
    public void WindowAvgInt_KeepsFraction_MatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<double> actual = db.Table<WinAvgRow>()
            .Select(r => SQLiteWindowFunctions.Avg(r.IntAmt).Over().AsValue())
            .ToList();

        double expected = new[] { 2, 3, 4 }.Average();
        Assert.Equal(3.0, expected);
        Assert.All(actual, a => Assert.Equal(expected, a));
    }

    [Fact]
    public void WindowAvgLong_KeepsFraction_MatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<double> actual = db.Table<WinAvgRow>()
            .Select(r => SQLiteWindowFunctions.Avg(r.LongAmt).Over().AsValue())
            .ToList();

        double expected = new[] { 2L, 3L, 4L }.Average();
        Assert.All(actual, a => Assert.Equal(expected, a));
    }

    [Fact]
    public void WindowAvgInt_OddSum_KeepsHalf()
    {
        using TestDatabase db = new();
        db.Table<WinAvgRow>().Schema.CreateTable();
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 1, IntAmt = 2 });
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 2, IntAmt = 3 });

        List<double> actual = db.Table<WinAvgRow>()
            .Select(r => SQLiteWindowFunctions.Avg(r.IntAmt).Over().AsValue())
            .ToList();

        Assert.All(actual, a => Assert.Equal(2.5, a));
    }

    [Fact]
    public void WindowAvgNullableInt_IgnoresNull_MatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<double?> actual = db.Table<WinAvgRow>()
            .Select(r => SQLiteWindowFunctions.Avg(r.NullAmt).Over().AsValue())
            .ToList();

        double? expected = new int?[] { 2, null, 4 }.Average();
        Assert.Equal(3.0, expected);
        Assert.All(actual, a => Assert.Equal(expected, a));
    }

    [Fact]
    public void WindowAvgDouble_StillWorks_MatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<double> actual = db.Table<WinAvgRow>()
            .Select(r => SQLiteWindowFunctions.Avg(r.DblAmt).Over().AsValue())
            .ToList();

        double expected = new[] { 2.0, 3.0, 4.0 }.Average();
        Assert.All(actual, a => Assert.Equal(expected, a));
    }

    [Fact]
    public void WindowAvgInt_Partitioned_KeepsFraction()
    {
        using TestDatabase db = new();
        db.Table<WinAvgRow>().Schema.CreateTable();
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 1, Grp = 1, IntAmt = 2 });
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 2, Grp = 1, IntAmt = 3 });
        db.Table<WinAvgRow>().Add(new WinAvgRow { Id = 3, Grp = 2, IntAmt = 10 });

        List<(int Id, double Avg)> actual = db.Table<WinAvgRow>()
            .Select(r => new { r.Id, Avg = SQLiteWindowFunctions.Avg(r.IntAmt).PartitionBy(r.Grp).AsValue() })
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => (r.Id, r.Avg))
            .ToList();

        Assert.Equal(2.5, actual[0].Avg);
        Assert.Equal(2.5, actual[1].Avg);
        Assert.Equal(10.0, actual[2].Avg);
    }
}
