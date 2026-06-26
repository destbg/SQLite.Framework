using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TsFromRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class TimeSpanFromUnitMultiArgumentParityTests
{
    private static TestDatabase Seed(int a, int b)
    {
        TestDatabase db = new();
        db.Table<TsFromRow>().Schema.CreateTable();
        db.Table<TsFromRow>().Add(new TsFromRow { Id = 1, A = a, B = b });
        return db;
    }

    [Fact]
    public void FromHours_HoursAndMinutes_MatchesDotNet()
    {
        using TestDatabase db = Seed(2, 30);

        long expected = new[] { new { A = 2, B = 30 } }.Select(r => TimeSpan.FromHours(r.A, r.B).Ticks).First();
        long actual = db.Table<TsFromRow>().Where(r => r.Id == 1).Select(r => TimeSpan.FromHours(r.A, r.B).Ticks).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromDays_DaysAndHours_MatchesDotNet()
    {
        using TestDatabase db = Seed(1, 12);

        long expected = new[] { new { A = 1, B = 12 } }.Select(r => TimeSpan.FromDays(r.A, r.B).Ticks).First();
        long actual = db.Table<TsFromRow>().Where(r => r.Id == 1).Select(r => TimeSpan.FromDays(r.A, r.B).Ticks).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromMinutes_MinutesAndSeconds_MatchesDotNet()
    {
        using TestDatabase db = Seed(3, 45);

        long expected = new[] { new { A = 3, B = 45 } }.Select(r => TimeSpan.FromMinutes(r.A, r.B).Ticks).First();
        long actual = db.Table<TsFromRow>().Where(r => r.Id == 1).Select(r => TimeSpan.FromMinutes(r.A, r.B).Ticks).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromHours_SingleArgument_MatchesDotNet()
    {
        using TestDatabase db = Seed(2, 30);

        long expected = new[] { new { A = 2 } }.Select(r => TimeSpan.FromHours(r.A).Ticks).First();
        long actual = db.Table<TsFromRow>().Where(r => r.Id == 1).Select(r => TimeSpan.FromHours(r.A).Ticks).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromHours_AllConstant_MatchesDotNet()
    {
        using TestDatabase db = Seed(2, 30);

        long expected = TimeSpan.FromHours(2, 30).Ticks;
        long actual = db.Table<TsFromRow>().Where(r => r.Id == 1).Select(r => TimeSpan.FromHours(2, 30).Ticks).First();

        Assert.Equal(expected, actual);
    }
}
