using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongToDoubleCastAverageTests
{
    [Fact]
    public void AverageOverDoubleCastUlongColumnMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = 1 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ULongValue = ulong.MaxValue });

        ulong[] seed = { 1ul, ulong.MaxValue };
        double oracle = seed.Average(v => (double)v);

        Assert.Equal(9223372036854775808.0, oracle);

        double actual = db.Table<NumericType>().Average(x => (double)x.ULongValue);

        Assert.Equal(oracle, actual);
    }
}
