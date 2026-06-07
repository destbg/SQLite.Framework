using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DecimalModuloTests
{
    [Fact]
    public void DecimalModuloKeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DecimalValue = 5.5m });

        decimal actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DecimalValue % 2m).First();

        Assert.Equal(5.5m % 2m, actual);
    }

    [Fact]
    public void DoubleModuloKeepsFractionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 5.5 });

        double actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DoubleValue % 2.0).First();

        Assert.Equal(5.5 % 2.0, actual);
    }
}
