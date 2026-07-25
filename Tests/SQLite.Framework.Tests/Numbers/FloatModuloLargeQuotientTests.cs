using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatModuloLargeQuotientTests
{
    [Fact]
    public void FloatModuloWithLargeQuotientDiffersFromDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 1e20 });

        List<NumericType> seed = [new NumericType { Id = 1, DoubleValue = 1e20 }];

        double oracle = seed.Where(x => x.Id == 1).Select(x => x.DoubleValue % 3.0).First();
        double actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DoubleValue % 3.0).First();

        Assert.Equal(1.0, oracle);
        Assert.NotEqual(oracle, actual);
    }
}
