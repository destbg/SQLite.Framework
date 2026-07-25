using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowOrderByUlongUnsignedOrderTests
{
    private static List<NumericType> Rows() =>
    [
        new NumericType { Id = 1, ULongValue = 1 },
        new NumericType { Id = 2, ULongValue = ulong.MaxValue },
        new NumericType { Id = 3, ULongValue = 2 },
        new NumericType { Id = 4, ULongValue = 9223372036854775808UL },
    ];

    [Fact]
    public void RowNumberOrderedByUlongUsesUnsignedOrder()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(Rows());

        List<long> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (long)Rows().Count(o => o.ULongValue < r.ULongValue) + 1).ToList();
        Assert.Equal([1, 4, 2, 3], expected);

        List<long> actual = db.Table<NumericType>()
            .Select(x => new { x.Id, R = SQLiteWindowFunctions.RowNumber().Over().OrderBy(x.ULongValue).AsValue() })
            .OrderBy(x => x.Id)
            .Select(x => x.R)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
