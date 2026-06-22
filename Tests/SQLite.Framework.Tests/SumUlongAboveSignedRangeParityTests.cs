using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SumUlongAboveSignedRangeParityTests
{
    [Fact]
    public void WindowSumUlong_TotalCrossesSignedRange_Throws()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 9041590448266294874UL });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = 635437757171324238UL });

        Assert.Throws<SQLiteException>(() => db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Sum(r.ULongValue).Over().AsValue())
            .ToList());
    }
}
