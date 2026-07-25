using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetOperationOrderingSemanticsTests
{
    private static void Seed(TestDatabase db)
    {
        db.Table<NumericType>().Schema.CreateTable();
        int[] firstValues = [5, 3, 5, 1, 3];
        for (int i = 0; i < firstValues.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = firstValues[i] });
        }

        db.Table<NumericType>().Add(new NumericType { Id = 6, IntValue = 7 });
    }

    [Fact]
    public void Union_ReturnsValuesInSortedOrder()
    {
        using TestDatabase db = new();
        Seed(db);

        int[] first = [5, 3, 5, 1, 3];
        int[] second = [7];
        List<int> expected = first.Union(second).OrderBy(v => v).ToList();

        List<int> actual = db.Table<NumericType>().Where(x => x.Id <= 5).Select(x => x.IntValue)
            .Union(db.Table<NumericType>().Where(x => x.Id == 6).Select(x => x.IntValue))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Except_ReturnsValuesInSortedOrder()
    {
        using TestDatabase db = new();
        Seed(db);

        int[] first = [5, 3, 5, 1, 3];
        int[] second = [99];
        List<int> expected = first.Except(second).OrderBy(v => v).ToList();

        List<int> actual = db.Table<NumericType>().Where(x => x.Id <= 5).Select(x => x.IntValue)
            .Except(db.Table<NumericType>().Where(x => x.IntValue == 99).Select(x => x.IntValue))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
