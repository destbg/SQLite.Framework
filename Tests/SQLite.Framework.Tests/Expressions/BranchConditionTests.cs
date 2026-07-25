using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BranchConditionTests
{
    [Fact]
    public void DecimalModuloKeepsFraction()
    {
        using TestDatabase db = new();
        db.Table<ProductLine>().Schema.CreateTable();
        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5.5m, Quantity = 1, Total = 0m });

        decimal actual = db.Table<ProductLine>().Select(p => p.Price % 2m).Single();

        Assert.Equal(5.5m % 2m, actual);
    }

    [Fact]
    public void FloatModuloKeepsFraction()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, FloatValue = 5.5f });

        float actual = db.Table<NumericType>().Select(n => n.FloatValue % 2f).Single();

        Assert.Equal(5.5f % 2f, actual);
    }

    [Fact]
    public void NullableColumnOnRightOfEqualityKeepsNullSafeSemantics()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\",\"Value\") VALUES (1,NULL),(2,5),(3,7)", []).ExecuteNonQuery();

        List<int> actual = db.Table<NullableEntity>().Where(x => 5 == x.Value).Select(x => x.Id).ToList();

        Assert.Equal(new[] { 2 }, actual);
    }

    [Fact]
    public void NullableCapturedConstantComparedToColumnKeepsNullSafeSemantics()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\",\"Value\") VALUES (1,NULL),(2,5),(3,7)", []).ExecuteNonQuery();

        int? threshold = 5;
        List<NullableEntity> oracle =
        [
            new() { Id = 1, Value = null },
            new() { Id = 2, Value = 5 },
            new() { Id = 3, Value = 7 }
        ];

        List<int> constantOnLeft = db.Table<NullableEntity>().Where(x => threshold == x.Value).Select(x => x.Id).ToList();
        List<int> constantOnRight = db.Table<NullableEntity>().Where(x => x.Id == threshold).Select(x => x.Id).ToList();

        Assert.Equal(oracle.Where(x => threshold == x.Value).Select(x => x.Id).ToList(), constantOnLeft);
        Assert.Equal(oracle.Where(x => x.Id == threshold).Select(x => x.Id).ToList(), constantOnRight);
    }
}
