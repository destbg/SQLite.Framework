using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongInlineLiteralStorageTests
{
    [Fact]
    public void UlongAboveLongMaxViaInlineLiteralRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        ulong big = 9223372036854775809UL;

        db.Table<NumericType>()
            .WithColumns(c => c.Set(x => x.ULongValue, big))
            .Add(new NumericType { Id = 1 });

        ulong oracle = big;
        Assert.Equal(9223372036854775809UL, oracle);

        ulong actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.ULongValue).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void UlongAboveLongMaxViaInlineLiteralMatchesBindPath()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        ulong big = 9223372036854775809UL;

        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = big });
        db.Table<NumericType>()
            .WithColumns(c => c.Set(x => x.ULongValue, big))
            .Add(new NumericType { Id = 2 });

        ulong bindPath = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.ULongValue).First();
        ulong inlinePath = db.Table<NumericType>().Where(x => x.Id == 2).Select(x => x.ULongValue).First();

        Assert.Equal(big, bindPath);
        Assert.Equal(bindPath, inlinePath);
    }
}
