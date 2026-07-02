using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WidenedIntegerSubtractionOverflowTests
{
    [Fact]
    public void WidenedSubtractionKeepsFullSixtyFourBitResult()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = -2000000000 });

        Assert.Equal(294967296L, unchecked((long)(-2000000000 - 2000000000)));

        long actual = db.Table<NumericType>().Select(x => (long)(x.IntValue - 2000000000)).Single();

        Assert.Equal(-4000000000L, actual);
    }
}
