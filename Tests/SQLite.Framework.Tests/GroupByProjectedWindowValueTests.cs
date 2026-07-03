using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByProjectedWindowValueTests
{
    private static List<NumericType> Rows() =>
    [
        new NumericType { Id = 1, IntValue = 5 },
        new NumericType { Id = 2, IntValue = 5 },
        new NumericType { Id = 3, IntValue = 5 },
        new NumericType { Id = 4, IntValue = 7 },
        new NumericType { Id = 5, IntValue = 7 },
    ];

    [Fact]
    public void GroupByRankValueReturnsDistinctRanks()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(Rows());

        List<long> expected = Rows()
            .Select(r => (long)Rows().Count(o => o.IntValue < r.IntValue) + 1)
            .Distinct().OrderBy(k => k).ToList();
        Assert.Equal([1, 4], expected);

        List<long> actual = db.Table<NumericType>()
            .Select(x => new { x.Id, R = SQLiteWindowFunctions.Rank().Over().OrderBy(x.IntValue).AsValue() })
            .GroupBy(x => x.R)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
