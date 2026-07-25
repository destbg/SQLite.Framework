using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathClampReversedBoundsTests
{
    [Fact]
    public void MathClampWithRuntimeReversedBoundsReturnsMinInsteadOfThrowing()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 5, ShortValue = 10, ByteValue = 3 });

        Assert.Throws<ArgumentException>(() => Math.Clamp(5, 10, 3));

        int actual = db.Table<NumericType>()
            .Where(x => x.Id == 1)
            .Select(x => Math.Clamp(x.IntValue, (int)x.ShortValue, (int)x.ByteValue))
            .First();

        Assert.Equal(10, actual);
    }
}
