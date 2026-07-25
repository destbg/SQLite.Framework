using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongExceptOrderTests
{
    [Fact]
    public void Except_OverUlongColumn_SortsBySignedStorage()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 1UL });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = 2UL });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 9223372036854775808UL });

        ulong[] firstSide = [1UL, 9223372036854775808UL];
        List<ulong> limited = firstSide.OrderBy(v => unchecked((long)v)).ToList();
        Assert.Equal(new ulong[] { 9223372036854775808UL, 1UL }, limited);

        List<ulong> actual = db.Table<NumericType>().Where(n => n.Id == 1 || n.Id == 3).Select(n => n.ULongValue)
            .Except(db.Table<NumericType>().Where(n => n.Id == 2).Select(n => n.ULongValue))
            .ToList();

        Assert.Equal(limited, actual);
    }
}
