using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongSetOperationOrderTests
{
    [Fact]
    public void Union_OverUlongColumn_SortsBySignedStorage()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 1UL });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = 2UL });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 9223372036854775808UL });
        db.Table<NumericType>().Add(new NumericType { Id = 4, ULongValue = 18446744073709551615UL });

        ulong[] all = [1UL, 9223372036854775808UL, 2UL, 18446744073709551615UL];
        List<ulong> limited = all.Distinct().OrderBy(v => unchecked((long)v)).ToList();
        Assert.Equal(new ulong[] { 9223372036854775808UL, 18446744073709551615UL, 1UL, 2UL }, limited);

        List<ulong> actual = db.Table<NumericType>().Where(n => n.Id == 1 || n.Id == 3).Select(n => n.ULongValue)
            .Union(db.Table<NumericType>().Where(n => n.Id == 2 || n.Id == 4).Select(n => n.ULongValue))
            .ToList();

        Assert.Equal(limited, actual);
    }
}
