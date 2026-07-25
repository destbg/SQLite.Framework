using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CastUnboxIntToLongTests
{
    [Fact]
    public void CastUnbox_IntColumnToLong_ShouldThrowLikeLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 100000 });

        List<NumericType> seed = new() { new NumericType { Id = 1, IntValue = 100000 } };

        Assert.Throws<InvalidCastException>(() =>
            seed.AsQueryable().Select(x => x.IntValue).Cast<long>().ToList());

        List<long> frameworkResult = db.Table<NumericType>().Select(x => x.IntValue).Cast<long>().ToList();

        Assert.Equal(new List<long> { 100000L }, frameworkResult);
    }
}