using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25oReverseOrderRows")]
public class H25oReverseOrderRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class OrderByAfterReverseAndDistinctTests
{
    [Fact]
    public void OrderByAfterReverseAndDistinctSortsTheValuesAscending()
    {
        using TestDatabase db = Setup(nameof(OrderByAfterReverseAndDistinctSortsTheValuesAscending));

        List<int> expected = Rows()
            .Select(r => r.Value)
            .Reverse()
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H25oReverseOrderRow>()
            .Select(r => r.Value)
            .Reverse()
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingAfterReverseAndDistinctSortsTheValuesDescending()
    {
        using TestDatabase db = Setup(nameof(OrderByDescendingAfterReverseAndDistinctSortsTheValuesDescending));

        List<int> expected = Rows()
            .Select(r => r.Value)
            .Reverse()
            .Distinct()
            .OrderByDescending(v => v)
            .ToList();

        List<int> actual = db.Table<H25oReverseOrderRow>()
            .Select(r => r.Value)
            .Reverse()
            .Distinct()
            .OrderByDescending(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByAfterReverseAndDistinctOverWholeRowsSortsTheRowsAscending()
    {
        using TestDatabase db = Setup(nameof(OrderByAfterReverseAndDistinctOverWholeRowsSortsTheRowsAscending));

        List<int> expected = Rows()
            .AsEnumerable()
            .Reverse()
            .Distinct()
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H25oReverseOrderRow>()
            .Reverse()
            .Distinct()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25oReverseOrderRow> Rows()
    {
        return
        [
            new H25oReverseOrderRow { Id = 1, Value = 5 },
            new H25oReverseOrderRow { Id = 2, Value = 3 },
            new H25oReverseOrderRow { Id = 3, Value = 5 },
            new H25oReverseOrderRow { Id = 4, Value = 1 },
            new H25oReverseOrderRow { Id = 5, Value = 4 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25oReverseOrderRow>().Schema.CreateTable();
        db.Table<H25oReverseOrderRow>().AddRange(Rows());
        return db;
    }
}
