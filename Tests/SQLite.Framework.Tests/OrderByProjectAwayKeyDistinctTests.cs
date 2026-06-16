using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class OrderedDistinctRow
{
    [Key]
    public int Id { get; set; }

    public int? Category { get; set; }

    public int SortValue { get; set; }
}

public class OrderByProjectAwayKeyDistinctTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<OrderedDistinctRow>().Schema.CreateTable();
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 1, Category = 1, SortValue = 10 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 2, Category = 1, SortValue = 20 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 3, Category = 2, SortValue = 30 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 4, Category = null, SortValue = 40 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 5, Category = null, SortValue = 50 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 6, Category = 2, SortValue = 5 });
        db.Table<OrderedDistinctRow>().Add(new OrderedDistinctRow { Id = 7, Category = 1, SortValue = 99 });
        return db;
    }

    [Fact]
    public void OrderByThenProjectAwayKeyThenDistinctReturnsCorrectDistinctValues()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<OrderedDistinctRow>().AsEnumerable()
            .Select(x => x.Category ?? -1)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([-1, 1, 2], expected);

        List<int> actual = db.Table<OrderedDistinctRow>()
            .OrderBy(x => x.SortValue)
            .Select(x => x.Category ?? -1)
            .Distinct()
            .ToList()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
