using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CapturedNullableUnaryParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<NullableBoolEntity>().Schema.CreateTable();
        db.Table<NullableBoolEntity>().Add(new NullableBoolEntity { Id = 1, Flag = true });
        db.Table<NullableBoolEntity>().Add(new NullableBoolEntity { Id = 2, Flag = false });
        db.Table<NullableBoolEntity>().Add(new NullableBoolEntity { Id = 3, Flag = null });
        return db;
    }

    [Fact]
    public void LogicalNotOfNullCapturedNullableBool_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        bool? wanted = null;

        List<NullableBoolEntity> seed =
        [
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
            new NullableBoolEntity { Id = 3, Flag = null }
        ];
        List<int> expected = seed.Where(x => x.Flag == !wanted).Select(x => x.Id).ToList();

        List<int> actual = db.Table<NullableBoolEntity>()
            .Where(x => x.Flag == !wanted)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegateOfNullCapturedNullableInt_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = -5 });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = null });
        int? threshold = null;

        List<NullableEntity> seed =
        [
            new NullableEntity { Id = 1, Value = -5 },
            new NullableEntity { Id = 2, Value = null }
        ];
        List<int> expected = seed.Where(x => x.Value == -threshold).Select(x => x.Id).ToList();

        List<int> actual = db.Table<NullableEntity>()
            .Where(x => x.Value == -threshold)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
