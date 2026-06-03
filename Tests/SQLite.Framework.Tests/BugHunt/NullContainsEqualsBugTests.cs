using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class NullContainsEqualsBugTests
{
    [Fact]
    public void NullableEquals_NullConstant_Projection_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = null });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = 10 });

        NullableEntity[] seed =
        [
            new NullableEntity { Id = 1, Value = null },
            new NullableEntity { Id = 2, Value = 10 },
        ];

        int? other = null;
        List<bool> expected = seed.OrderBy(x => x.Id).Select(x => x.Value.Equals(other)).ToList();
        List<bool> actual = db.Table<NullableEntity>().OrderBy(x => x.Id).Select(x => x.Value.Equals(other)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableEquals_ColumnToColumn_Where_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        TwoNullableIntEntity[] rows =
        [
            new TwoNullableIntEntity { Id = 1, A = null, B = null },
            new TwoNullableIntEntity { Id = 2, A = 5, B = 5 },
            new TwoNullableIntEntity { Id = 3, A = 5, B = 7 },
            new TwoNullableIntEntity { Id = 4, A = null, B = 5 },
        ];
        foreach (TwoNullableIntEntity r in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        List<int> expected = rows.Where(p => p.A.Equals(p.B)).Select(p => p.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(p => p.A.Equals(p.B)).Select(p => p.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableRelationalProjectedToBoolNullable_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = null });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = 10 });

        (int Id, int? Value)[] seed = [(1, null), (2, 10)];
        List<bool?> expected = seed.OrderBy(r => r.Id).Select(r => (bool?)(r.Value > 5)).ToList();
        List<bool?> actual = db.Table<NullableEntity>().OrderBy(x => x.Id).Select(x => (bool?)(x.Value > 5)).ToList();

        Assert.Equal(expected, actual);
    }
}
