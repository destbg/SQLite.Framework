using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongGroupAggregateWithFilterTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 1, ULongValue = 5 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, IntValue = 1, ULongValue = ulong.MaxValue });
        db.Table<NumericType>().Add(new NumericType { Id = 3, IntValue = 1, ULongValue = 100 });
        return db;
    }

    [Fact]
    public void GroupMax_UlongWithFilter_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<NumericType> seed =
        [
            new NumericType { Id = 1, IntValue = 1, ULongValue = 5 },
            new NumericType { Id = 2, IntValue = 1, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, IntValue = 1, ULongValue = 100 },
        ];

        ulong oracle = seed.GroupBy(x => x.IntValue)
            .Select(g => g.Where(x => x.Id > 0).Max(x => x.ULongValue))
            .First();

        ulong actual = db.Table<NumericType>()
            .GroupBy(x => x.IntValue)
            .Select(g => g.Where(x => x.Id > 0).Max(x => x.ULongValue))
            .First();

        Assert.Equal(ulong.MaxValue, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupMin_UlongWithFilter_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<NumericType> seed =
        [
            new NumericType { Id = 1, IntValue = 1, ULongValue = 5 },
            new NumericType { Id = 2, IntValue = 1, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, IntValue = 1, ULongValue = 100 },
        ];

        ulong oracle = seed.GroupBy(x => x.IntValue)
            .Select(g => g.Where(x => x.Id > 0).Min(x => x.ULongValue))
            .First();

        ulong actual = db.Table<NumericType>()
            .GroupBy(x => x.IntValue)
            .Select(g => g.Where(x => x.Id > 0).Min(x => x.ULongValue))
            .First();

        Assert.Equal(5UL, oracle);
        Assert.Equal(oracle, actual);
    }
}
