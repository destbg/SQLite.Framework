using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableComparisonProjectionSemanticsTests
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
    public void NullableInequalityProjectedToListMatchesDotNet()
    {
        var rows = new[] { (1, (int?)null), (2, (int?)10) };
        using TestDatabase db = Seed(rows);

        List<bool> actual = db.Table<NullableEntity>().OrderBy(x => x.Id).Select(x => x.Value > 5).ToList();
        List<bool> oracle = rows.Select(r => r.Item2 > 5).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableEqualityProjectedToScalarMatchesDotNet()
    {
        using TestDatabase db = Seed((1, null));

        int? source = null;
        bool actual = db.Table<NullableEntity>().Where(x => x.Id == 1).Select(x => x.Value == 5).First();
        bool oracle = source == 5;

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableInequalityProjectedToScalarMatchesDotNet()
    {
        using TestDatabase db = Seed((1, null));

        int? source = null;
        bool actual = db.Table<NullableEntity>().Where(x => x.Id == 1).Select(x => x.Value > 5).First();
        bool oracle = source > 5;

        Assert.Equal(oracle, actual);
    }
}
