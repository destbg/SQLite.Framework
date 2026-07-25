using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowAvgNarrowIntegerParityTests
{
    [Fact]
    public void WindowAvg_ShortColumn_KeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ShortValue = 2 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ShortValue = 3 });

        double expected = new[] { 2, 3 }.Average();
        List<double> actual = db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Avg(r.ShortValue).Over().AsValue())
            .ToList();

        Assert.All(actual, a => Assert.Equal(expected, a));
    }

    [Fact]
    public void WindowAvg_ByteColumn_KeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ByteValue = 2 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ByteValue = 3 });

        double expected = new[] { 2, 3 }.Average();
        List<double> actual = db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Avg(r.ByteValue).Over().AsValue())
            .ToList();

        Assert.All(actual, a => Assert.Equal(expected, a));
    }
}
