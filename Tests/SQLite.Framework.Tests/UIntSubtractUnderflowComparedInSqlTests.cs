using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UIntSubtractUnderflowComparedInSqlTests
{
    [Fact]
    public void UIntSubtractUnderflowComparedInWhereMatchesDotNet()
    {
        (int Id, uint Value)[] seed = [(1, 0u), (2, 200u)];

        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        foreach ((int id, uint value) in seed)
        {
            db.Table<NumericType>().Add(new NumericType { Id = id, UIntValue = value });
        }

        List<int> oracle = seed
            .Where(x => (x.Value - 1u) > 100u)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<NumericType>()
            .Where(x => (x.UIntValue - 1u) > 100u)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 2], oracle);
        Assert.Equal(oracle, actual);
    }
}
