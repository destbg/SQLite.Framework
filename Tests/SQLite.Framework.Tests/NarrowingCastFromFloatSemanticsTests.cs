using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NarrowingCastFromFloatSemanticsTests
{
    [Fact]
    public void DoubleToInt_OutOfRange_ThrowsOverflow()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 3000000000.0 });

        Assert.Throws<OverflowException>(() =>
            db.Table<NumericType>().Select(x => (int)x.DoubleValue).ToList());
    }

    [Fact]
    public void DoubleToShort_OutOfRange_ThrowsOverflow()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 70000.0 });

        Assert.Throws<OverflowException>(() =>
            db.Table<NumericType>().Select(x => (short)x.DoubleValue).ToList());
    }
}
