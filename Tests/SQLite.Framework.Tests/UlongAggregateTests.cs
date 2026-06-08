using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongAggregateTests
{
    [Fact]
    public void MaxOverUlongAboveSignedRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 1 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = ulong.MaxValue });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 100 });

        List<NumericType> seed =
        [
            new NumericType { Id = 1, ULongValue = 1 },
            new NumericType { Id = 2, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, ULongValue = 100 },
        ];

        ulong oracle = seed.Max(x => x.ULongValue);
        ulong actual = db.Table<NumericType>().Max(x => x.ULongValue);

        Assert.Equal(ulong.MaxValue, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MinOverUlongAboveSignedRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 5 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = ulong.MaxValue });
        db.Table<NumericType>().Add(new NumericType { Id = 3, ULongValue = 100 });

        List<NumericType> seed =
        [
            new NumericType { Id = 1, ULongValue = 5 },
            new NumericType { Id = 2, ULongValue = ulong.MaxValue },
            new NumericType { Id = 3, ULongValue = 100 },
        ];

        ulong oracle = seed.Min(x => x.ULongValue);
        ulong actual = db.Table<NumericType>().Min(x => x.ULongValue);

        Assert.Equal(5UL, oracle);
        Assert.Equal(oracle, actual);
    }
}
