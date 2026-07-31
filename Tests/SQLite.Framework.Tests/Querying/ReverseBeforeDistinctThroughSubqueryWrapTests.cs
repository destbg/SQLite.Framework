using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26cWrappedReverseDistinctRows")]
public class H26cWrappedReverseDistinctRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class ReverseBeforeDistinctThroughSubqueryWrapTests
{
    [Fact]
    public void SelectAfterReverseAndDistinctDoesNotReadTheUnreversedValues()
    {
        using TestDatabase db = Setup(nameof(SelectAfterReverseAndDistinctDoesNotReadTheUnreversedValues));

        List<int> expected = Rows()
            .Select(r => r.Amount)
            .Reverse()
            .Distinct()
            .Select(v => v * 2)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H26cWrappedReverseDistinctRow>()
            .Select(r => r.Amount)
            .Reverse()
            .Distinct()
            .Select(v => v * 2)
            .ToList());
    }

    private static void AssertMatchesOrIsRefused<T>(List<T> expected, Func<List<T>> run)
    {
        List<T> actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H26cWrappedReverseDistinctRow> Rows()
    {
        return
        [
            new H26cWrappedReverseDistinctRow { Id = 1, Amount = 3 },
            new H26cWrappedReverseDistinctRow { Id = 2, Amount = 1 },
            new H26cWrappedReverseDistinctRow { Id = 3, Amount = 3 },
            new H26cWrappedReverseDistinctRow { Id = 4, Amount = 2 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26cWrappedReverseDistinctRow>().Schema.CreateTable();
        db.Table<H26cWrappedReverseDistinctRow>().AddRange(Rows());
        return db;
    }
}
