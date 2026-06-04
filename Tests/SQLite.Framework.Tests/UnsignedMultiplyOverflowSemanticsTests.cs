using System;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UnsignedMultiplyOverflowSemanticsTests
{
    [Fact]
    public void UIntMultiplyOverflowingInt64Throws()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, UIntValue = uint.MaxValue });

        Assert.Throws<OverflowException>(() => db.Table<NumericType>().Select(x => x.UIntValue * x.UIntValue).First());
    }

    [Fact]
    public void ULongMultiplyOverflowingInt64Throws()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 5000000000UL });

        Assert.Throws<OverflowException>(() => db.Table<NumericType>().Select(x => x.ULongValue * x.ULongValue).First());
    }
}
