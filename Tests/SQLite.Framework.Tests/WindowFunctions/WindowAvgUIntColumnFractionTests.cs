using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowAvgUIntColumnFractionTests
{
    [Fact]
    public void WindowAvgUIntColumn_KeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, UIntValue = 1 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, UIntValue = 2 });
        db.Table<NumericType>().Add(new NumericType { Id = 3, UIntValue = 4 });

        uint[] seed = { 1u, 2u, 4u };
        double expected = seed.Select(v => (long)v).Average();
        Assert.Equal(2.3333333333333335, expected);

        List<double> framework = db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Avg(r.UIntValue).Over().AsValue())
            .ToList();

        Assert.Equal(3, framework.Count);
        Assert.Equal(expected, framework[0]);
    }
}
