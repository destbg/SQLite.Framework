using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EqualsLiftedComparisonOperandTests
{
    [Fact]
    public void EqualsFalseOnNullLiftedComparisonKeepsRow()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        List<NullableEntity> rows =
        [
            new() { Id = 1, Value = null },
            new() { Id = 2, Value = 10 },
        ];
        db.Table<NullableEntity>().AddRange(rows);

        List<int> expected = rows
            .Where(x => (x.Value > 5).Equals(false))
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<NullableEntity>()
            .Where(x => (x.Value > 5).Equals(false))
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
