using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class IntegerArithmeticOverflowOracleBugTests
{
    private static TestDatabase Seed(NumericType row)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(row);
        return db;
    }

    [Fact]
    public void IntMultiplyOverflowWrapsLikeDotNet()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = 100000 });

        int actual = db.Table<NumericType>().Select(x => x.IntValue * x.IntValue).First();

        Assert.Equal(unchecked(100000 * 100000), actual);
    }

    [Fact]
    public void IntAddOverflowWrapsLikeDotNet()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = int.MaxValue });

        int actual = db.Table<NumericType>().Select(x => x.IntValue + 1).First();

        Assert.Equal(unchecked(int.MaxValue + 1), actual);
    }

    [Fact]
    public void IntLeftShiftOverflowWrapsLikeDotNet()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = 1 });

        int actual = db.Table<NumericType>().Select(x => x.IntValue << 31).First();

        Assert.Equal(1 << 31, actual);
    }

    [Fact]
    public void UIntAddOverflowWrapsLikeDotNet()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, UIntValue = 4000000000 });

        uint actual = db.Table<NumericType>().Select(x => x.UIntValue + x.UIntValue).First();

        Assert.Equal(unchecked(4000000000u + 4000000000u), actual);
    }

    [Fact]
    public void LongArithmeticDoesNotWrap()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, LongValue = 5000000000 });

        long actual = db.Table<NumericType>().Select(x => x.LongValue + x.LongValue).First();

        Assert.Equal(5000000000L + 5000000000L, actual);
    }
}
