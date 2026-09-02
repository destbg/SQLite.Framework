using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25pTicks")]
public class H25pTick
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class WindowFunctionInsideWherePredicateTests
{
    [Fact]
    public void DirectWindowPredicateReturnsTheIndependentlyRankedRows()
    {
        using TestDatabase db = Setup(nameof(DirectWindowPredicateReturnsTheIndependentlyRankedRows));
        List<H25pTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        int[] expected = rows
            .Where(r => rowNumbers[r.Id] <= 3 && r.Amount <= 20)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToArray();

        int[] actual = db.Table<H25pTick>()
            .Where(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 3
                && r.Amount <= 20)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DirectWindowGroupByKeyReturnsIndependentDenseRankGroups()
    {
        using TestDatabase db = Setup(nameof(DirectWindowGroupByKeyReturnsIndependentDenseRankGroups));
        List<H25pTick> rows = Rows();
        int[] expected = rows
            .GroupBy(r => r.Amount)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToArray();

        int[] actual = db.Table<H25pTick>()
            .GroupBy(r => SQLiteWindowFunctions.DenseRank().OrderBy(r.Amount).AsValue())
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderBy(g => g.Key)
            .Select(g => g.Count)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(7)]
    public void DirectWindowPredicateHonorsRankBoundaries(int maximumRank)
    {
        using TestDatabase db = Setup(nameof(DirectWindowPredicateHonorsRankBoundaries));
        List<H25pTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        int[] expected = rows
            .Where(r => rowNumbers[r.Id] <= maximumRank)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToArray();

        int[] actual = db.Table<H25pTick>()
            .Where(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= maximumRank)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DirectWindowPredicateAndGroupByReturnNoRowsForAnEmptySource()
    {
        using TestDatabase db = Setup(nameof(DirectWindowPredicateAndGroupByReturnNoRowsForAnEmptySource), addRows: false);
        List<H25pTick> rows = [];
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        List<H25pTick> expectedFiltered = rows.Where(r => rowNumbers[r.Id] <= 2).ToList();
        List<int> expectedGroups = rows.GroupBy(r => r.Amount).Select(g => g.Count()).ToList();

        List<H25pTick> filtered = db.Table<H25pTick>()
            .Where(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2)
            .ToList();
        List<int> groups = db.Table<H25pTick>()
            .GroupBy(r => SQLiteWindowFunctions.DenseRank().OrderBy(r.Amount).AsValue())
            .Select(g => g.Count())
            .ToList();

        Assert.Equal(expectedFiltered.Select(r => r.Id), filtered.Select(r => r.Id));
        Assert.Equal(expectedGroups, groups);
    }

    [Fact]
    public void WindowGroupByElementSelectorIsRejected()
    {
        using TestDatabase db = Setup(nameof(WindowGroupByElementSelectorIsRejected));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<H25pTick>()
            .GroupBy(
                r => SQLiteWindowFunctions.DenseRank().OrderBy(r.Amount).AsValue(),
                r => r.Id)
            .Select(g => g.Count())
            .ToList());

        Assert.Contains("GroupBy key", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowPredicateInsideNestedAnyStaysAtTheNestedQueryLevel()
    {
        using TestDatabase db = Setup(nameof(WindowPredicateInsideNestedAnyStaysAtTheNestedQueryLevel));
        List<H25pTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        int[] expected = rows
            .Where(outer => rows.Any(inner => inner.Id == outer.Id && rowNumbers[inner.Id] <= 3))
            .OrderBy(row => row.Id)
            .Select(row => row.Id)
            .ToArray();

        int[] actual = db.Table<H25pTick>()
            .Where(outer => db.Table<H25pTick>()
                .Any(inner => inner.Id == outer.Id
                    && SQLiteWindowFunctions.RowNumber()
                        .OrderBy(inner.Amount)
                        .ThenOrderBy(inner.Id)
                        .AsValue() <= 3))
            .OrderBy(row => row.Id)
            .Select(row => row.Id)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexedWindowPredicateHasAUsefulMessage()
    {
        using TestDatabase db = Setup(nameof(IndexedWindowPredicateHasAUsefulMessage));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<H25pTick>()
            .Where((row, index) => SQLiteWindowFunctions.RowNumber()
                .OrderBy(row.Amount)
                .ThenOrderBy(row.Id)
                .AsValue() <= index + 1)
            .ToList());

        Assert.Equal(
            "A window function in Where needs a one-parameter predicate that can be moved to an outer query.",
            exception.Message);
    }

    private static List<H25pTick> Rows()
    {
        return
        [
            new H25pTick { Id = 1, Amount = 20 },
            new H25pTick { Id = 2, Amount = -5 },
            new H25pTick { Id = 3, Amount = 20 },
            new H25pTick { Id = 4, Amount = 0 },
            new H25pTick { Id = 5, Amount = 30 },
            new H25pTick { Id = 6, Amount = -5 }
        ];
    }

    private static Dictionary<int, long> RowNumbers(IEnumerable<H25pTick> rows)
    {
        return rows
            .OrderBy(r => r.Amount)
            .ThenBy(r => r.Id)
            .Select((r, index) => (r.Id, RowNumber: (long)index + 1))
            .ToDictionary(r => r.Id, r => r.RowNumber);
    }

    private static TestDatabase Setup(string methodName, bool addRows = true)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25pTick>().Schema.CreateTable();
        if (addRows)
        {
            db.Table<H25pTick>().AddRange(Rows());
        }
        return db;
    }
}
