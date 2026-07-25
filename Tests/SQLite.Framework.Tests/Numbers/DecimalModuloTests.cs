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

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
    [Fact]
    public void DoubleModuloWithMathFunctionFloorMatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLite.Framework.Enums.SQLiteMinimumVersion.V3_35));
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 5.5 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, DoubleValue = -5.5 });

        List<double> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.DoubleValue % 2.0).ToList();

        Assert.Equal([5.5 % 2.0, -5.5 % 2.0], actual);
    }

    [Fact]
    public void FloatModuloWithMathFunctionFloorMatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLite.Framework.Enums.SQLiteMinimumVersion.V3_35));
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, FloatValue = 5.5f });

        float actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.FloatValue % 2f).First();

        Assert.Equal(5.5f % 2f, actual);
    }

    [Fact]
    public void DecimalModuloWithMathFunctionFloorMatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLite.Framework.Enums.SQLiteMinimumVersion.V3_35));
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DecimalValue = 5.5m });

        decimal actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DecimalValue % 2m).First();

        Assert.Equal(5.5m % 2m, actual);
    }

    [Fact]
    public void DoubleModuloBelowMathFunctionFloorMatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLite.Framework.Enums.SQLiteMinimumVersion.V3_22));
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 5.5 });

        double actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => x.DoubleValue % 2.0).First();

        Assert.Equal(5.5 % 2.0, actual);
    }
#endif
}
