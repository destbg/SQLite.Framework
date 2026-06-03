using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class NullContainsEqualsBugTests
{
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
