using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatingPointModuloLargeValueParityTests
{
    private static TestDatabase Seed(double d, float f, decimal m)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = d, FloatValue = f, DecimalValue = m });
        return db;
    }

    [Fact]
    public void DoubleModuloLargeValue_DoesNotReturnTrueRemainder()
    {
        using TestDatabase db = Seed(2e19, 0f, 0m);

        double trueRemainder = new[] { 2e19 }.Select(d => d % 2.0).First();
        double actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DoubleValue % 2.0).First();

        Assert.Equal(0.0, trueRemainder);
        Assert.NotEqual(trueRemainder, actual);
    }

    [Fact]
    public void FloatModuloLargeValue_DoesNotReturnTrueRemainder()
    {
        using TestDatabase db = Seed(0, 2e19f, 0m);

        float trueRemainder = new[] { 2e19f }.Select(f => f % 2f).First();
        float actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.FloatValue % 2f).First();

        Assert.Equal(0f, trueRemainder);
        Assert.NotEqual(trueRemainder, actual);
    }

    [Fact]
    public void DecimalModuloLargeValue_DoesNotReturnTrueRemainder()
    {
        using TestDatabase db = Seed(0, 0f, 2e19m);

        decimal trueRemainder = new[] { 2e19m }.Select(m => m % 2m).First();
        decimal actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DecimalValue % 2m).First();

        Assert.Equal(0m, trueRemainder);
        Assert.NotEqual(trueRemainder, actual);
    }

    [Fact]
    public void DoubleModuloSmallValue_MatchesDotNet()
    {
        using TestDatabase db = Seed(123.5, 0f, 0m);

        double expected = new[] { 123.5 }.Select(d => d % 10.0).First();
        double actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DoubleValue % 10.0).First();

        Assert.Equal(expected, actual);
    }
}
