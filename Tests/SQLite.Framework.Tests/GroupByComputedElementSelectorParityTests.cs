using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByComputedElementSelectorParityTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<GroupedSaleRow>().Schema.CreateTable();
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 1, Name = "a", Value = 10 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 2, Name = "a", Value = 20 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 3, Name = "b", Value = 5 });
        db.Table<GroupedSaleRow>().Add(new GroupedSaleRow { Id = 4, Name = "b", Value = 30 });
        return db;
    }

    [Fact]
    public void ComputedElementSelectorSumWithoutSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<GroupedSaleRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value * 2)
            .Select(g => g.Sum())
            .ToList();

        List<int> actual = db.Table<GroupedSaleRow>()
            .GroupBy(r => r.Name, r => r.Value * 2)
            .Select(g => g.Sum())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
