using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UIntMultiplyOverflowWidenedTests
{
    [Fact]
    public void UIntMultiplyOverflowThenWidenToLong_UsesFull64BitProduct()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, UIntValue = 100000 });

        long limited = 100000L * 100000L;
        Assert.Equal(10000000000L, limited);

        long actual = db.Table<NumericType>()
            .Select(x => (long)(x.UIntValue * x.UIntValue))
            .Single();

        Assert.Equal(limited, actual);
    }
}
