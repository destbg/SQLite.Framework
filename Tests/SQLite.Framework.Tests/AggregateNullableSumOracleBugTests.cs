using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AggregateNullableSumOracleBugTests
{
    private static TestDatabase Seed(params (int id, int? val)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? val) in rows)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = val });
        }

        return db;
    }

    [Fact]
    public void SumOfNullableColumnAllNullMatchesDotNet()
    {
        (int id, int? val)[] rows = { (1, null), (2, null) };
        using TestDatabase db = Seed(rows);

        int? actual = db.Table<NullableEntity>().Sum(x => x.Value);
        int? oracle = rows.Select(r => r.val).Sum();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SumOfNullableColumnEmptyTableMatchesDotNet()
    {
        using TestDatabase db = Seed();

        int? actual = db.Table<NullableEntity>().Sum(x => x.Value);
        int? oracle = Array.Empty<int?>().Sum();

        Assert.Equal(oracle, actual);
    }
}
