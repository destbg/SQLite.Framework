using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowAvgULongColumnFractionTests
{
    [Fact]
    public void WindowAvgULongColumn_KeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 1 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = 2 });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 4 });

        ulong[] seed = { 1ul, 2ul, 4ul };
        double expected = seed.Select(v => (long)v).Average();
        Assert.Equal(2.3333333333333335, expected);

        List<double> framework = db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Avg(r.ULongValue).Over().AsValue())
            .ToList();

        Assert.Equal(3, framework.Count);
        Assert.Equal(expected, framework[0]);
    }
}
