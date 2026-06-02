using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class SetOperationAnyBugTests
{
    [Fact]
    public void IntersectThenAnyNoPredicate()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType { Id = 1, IntValue = 5 },
            new NumericType { Id = 2, IntValue = 3 },
            new NumericType { Id = 3, IntValue = 5 },
            new NumericType { Id = 4, IntValue = 1 },
            new NumericType { Id = 5, IntValue = 3 },
        });

        List<int> rows = [5, 3, 5, 1, 3];
        bool expected = rows.Intersect(rows.Where(v => v == 5)).Any();

        IQueryable<int> a = db.Table<NumericType>().Select(x => x.IntValue);
        IQueryable<int> b = db.Table<NumericType>().Where(x => x.IntValue == 5).Select(x => x.IntValue);
        bool actual = a.Intersect(b).Any();

        Assert.Equal(expected, actual);
    }
}
