using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupedUlongMinMaxAggregateTests
{
    [Fact]
    public void GroupedMaxOverUlongAboveSignedRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 7, ULongValue = 1 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, IntValue = 7, ULongValue = ulong.MaxValue });
        db.Table<NumericType>().Add(new NumericType { Id = 3, IntValue = 7, ULongValue = 100 });

        NumericType[] seed =
        [
            new NumericType { Id = 1, IntValue = 7, ULongValue = 1 },
            new NumericType { Id = 2, IntValue = 7, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, IntValue = 7, ULongValue = 100 },
        ];

        ulong oracle = seed.GroupBy(n => n.IntValue).Select(g => g.Max(x => x.ULongValue)).Single();
        Assert.Equal(18446744073709551615UL, oracle);

        ulong actual = (
            from n in db.Table<NumericType>()
            group n by n.IntValue
            into g
            select g.Max(x => x.ULongValue)
        ).Single();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupedMinOverUlongAboveSignedRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 7, ULongValue = 5 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, IntValue = 7, ULongValue = ulong.MaxValue });
        db.Table<NumericType>().Add(new NumericType { Id = 3, IntValue = 7, ULongValue = 100 });

        NumericType[] seed =
        [
            new NumericType { Id = 1, IntValue = 7, ULongValue = 5 },
            new NumericType { Id = 2, IntValue = 7, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, IntValue = 7, ULongValue = 100 },
        ];

        ulong oracle = seed.GroupBy(n => n.IntValue).Select(g => g.Min(x => x.ULongValue)).Single();
        Assert.Equal(5UL, oracle);

        ulong actual = (
            from n in db.Table<NumericType>()
            group n by n.IntValue
            into g
            select g.Min(x => x.ULongValue)
        ).Single();

        Assert.Equal(oracle, actual);
    }
}