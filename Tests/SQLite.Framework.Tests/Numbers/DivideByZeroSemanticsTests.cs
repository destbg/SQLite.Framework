using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DivideByZeroSemanticsTests
{
    [Fact]
    public void IntegerDivisionByZeroYieldsZeroInNonNullableProjection()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 5, ShortValue = 0 });

        List<int> result = db.Table<NumericType>().Select(x => x.IntValue / x.ShortValue).ToList();

        Assert.Equal(new[] { 0 }, result);
    }

    [Fact]
    public void IntegerModuloByZeroYieldsZeroInNonNullableProjection()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 5, ShortValue = 0 });

        List<int> result = db.Table<NumericType>().Select(x => x.IntValue % x.ShortValue).ToList();

        Assert.Equal(new[] { 0 }, result);
    }

    [Fact]
    public void IntegerDivisionByZeroYieldsNullInNullableProjection()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 5, ShortValue = 0 });

        List<int?> result = db.Table<NumericType>().Select(x => (int?)(x.IntValue / x.ShortValue)).ToList();

        Assert.Equal(new int?[] { null }, result);
    }
}
