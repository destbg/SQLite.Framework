using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AllQuantifierNullSemanticsTests
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

    private static List<NullableEntity> Mem(params (int id, int? val)[] rows)
    {
        return rows.Select(r => new NullableEntity { Id = r.id, Value = r.val }).ToList();
    }

    [Fact]
    public void AllGreaterThanWithNullRowAndOthersPassingMatchesDotNet()
    {
        var rows = new[] { (1, (int?)null), (2, (int?)10) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().All(x => x.Value > 5);
        bool oracle = Mem(rows).All(x => x.Value > 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AllGreaterThanWithNullRowAndFailingRowMatchesDotNet()
    {
        var rows = new[] { (1, (int?)null), (2, (int?)3) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().All(x => x.Value > 5);
        bool oracle = Mem(rows).All(x => x.Value > 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AllLessThanOrEqualWithNullRowMatchesDotNet()
    {
        var rows = new[] { (1, (int?)null), (2, (int?)2) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().All(x => x.Value <= 5);
        bool oracle = Mem(rows).All(x => x.Value <= 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AllGreaterThanAllRowsPassingMatchesDotNet()
    {
        var rows = new[] { (1, (int?)8), (2, (int?)10) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().All(x => x.Value > 5);
        bool oracle = Mem(rows).All(x => x.Value > 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AllGreaterThanWithOneFailingRowMatchesDotNet()
    {
        var rows = new[] { (1, (int?)8), (2, (int?)3) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().All(x => x.Value > 5);
        bool oracle = Mem(rows).All(x => x.Value > 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AnyGreaterThanWithNullRowMatchesDotNet()
    {
        var rows = new[] { (1, (int?)null), (2, (int?)3) };
        using TestDatabase db = Seed(rows);

        bool actual = db.Table<NullableEntity>().Any(x => x.Value > 5);
        bool oracle = Mem(rows).Any(x => x.Value > 5);

        Assert.Equal(oracle, actual);
    }
}
