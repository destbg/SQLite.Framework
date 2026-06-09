using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowMaxUlongAboveSignedRangeTests
{
    [Fact]
    public void WindowMax_UlongColumn_AboveSignedRange_UsesSignedStorage()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 9223372036854775810UL });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = 5 });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 100 });

        ulong[] seed = [9223372036854775810UL, 5UL, 100UL];
        ulong limited = unchecked((ulong)seed.Select(v => unchecked((long)v)).Max());
        Assert.Equal(100UL, limited);

        List<ulong> actual = db.Table<NumericType>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Max(r.ULongValue).Over().AsValue())
            .ToList();

        Assert.All(actual, a => Assert.Equal(limited, a));
    }
}
