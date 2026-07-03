using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowRangeOffsetUlongSmallValuesTests
{
    [Fact]
    public void SumRangeNumericOffsetOrderedByUlongSmallValues()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        List<NumericType> rows =
        [
            new NumericType { Id = 1, IntValue = 1, ULongValue = 1 },
            new NumericType { Id = 2, IntValue = 3, ULongValue = 100 },
            new NumericType { Id = 3, IntValue = 2, ULongValue = 2 },
            new NumericType { Id = 4, IntValue = 4, ULongValue = 50 },
            new NumericType { Id = 5, IntValue = 5, ULongValue = 7 },
        ];
        db.Table<NumericType>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id)
            .Select(r => rows.Where(o => o.ULongValue >= r.ULongValue - Math.Min(r.ULongValue, 5UL) && o.ULongValue <= r.ULongValue).Sum(o => o.IntValue))
            .ToList();
        Assert.Equal([1, 3, 3, 4, 7], expected);

        List<int> actual = db.Table<NumericType>()
            .Select(x => new
            {
                x.Id,
                S = SQLiteWindowFunctions.Sum(x.IntValue).Over().OrderBy(x.ULongValue)
                    .Range(SQLiteFrameBoundary.Preceding(5), SQLiteFrameBoundary.CurrentRow()).AsValue()
            })
            .OrderBy(x => x.Id)
            .Select(x => x.S)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
