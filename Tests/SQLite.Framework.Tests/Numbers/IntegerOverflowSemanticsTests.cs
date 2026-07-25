using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class IntegerOverflowSemanticsTests
{
    private static TestDatabase Seed(params NumericType[] rows)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        foreach (NumericType r in rows) db.Table<NumericType>().Add(r);
        return db;
    }

    [Fact]
    public void IntProjectionOverflowThrows()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = 100000 });

        Assert.Throws<OverflowException>(
            () => db.Table<NumericType>().Select(x => x.IntValue * x.IntValue).First());
    }

    [Fact]
    public void IntAddOverflowThrows()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = int.MaxValue });

        Assert.Throws<OverflowException>(
            () => db.Table<NumericType>().Select(x => x.IntValue + 1).First());
    }

    [Fact]
    public void SumIntOverflowThrowsLikeDotNet()
    {
        using TestDatabase db = Seed(
            new NumericType { Id = 1, IntValue = 1_000_000_000 },
            new NumericType { Id = 2, IntValue = 1_000_000_000 },
            new NumericType { Id = 3, IntValue = 1_000_000_000 });

        Assert.Throws<OverflowException>(() => db.Table<NumericType>().Sum(x => x.IntValue));
        Assert.Throws<OverflowException>(
            () => new[] { 1_000_000_000, 1_000_000_000, 1_000_000_000 }.Sum());
    }

    [Fact]
    public void CastToLongComputesInSixtyFourBits()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, IntValue = 100000 });

        long actual = db.Table<NumericType>().Select(x => (long)x.IntValue * x.IntValue).First();

        Assert.Equal((long)100000 * 100000, actual);
    }

    [Fact]
    public void UIntProjectionOverflowWraps()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, UIntValue = 4000000000 });

        uint actual = db.Table<NumericType>().Select(x => x.UIntValue + x.UIntValue).First();

        Assert.Equal(unchecked(4000000000u + 4000000000u), actual);
    }

    [Fact]
    public void LongArithmeticInRangeMatchesDotNet()
    {
        using TestDatabase db = Seed(new NumericType { Id = 1, LongValue = 5000000000 });

        long actual = db.Table<NumericType>().Select(x => x.LongValue + x.LongValue).First();

        Assert.Equal(5000000000L + 5000000000L, actual);
    }
}
