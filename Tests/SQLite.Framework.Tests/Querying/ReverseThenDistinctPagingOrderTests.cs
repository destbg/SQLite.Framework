using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24aReverseDistinctRows")]
public class H24aReverseDistinctRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ReverseThenDistinctPagingOrderTests
{
    [Fact]
    public void TakeAfterReverseAndDistinctDoesNotReadTheUnreversedValues()
    {
        using TestDatabase db = Setup(nameof(TakeAfterReverseAndDistinctDoesNotReadTheUnreversedValues));

        List<string> expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .Take(2)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H24aReverseDistinctRow>()
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .Take(2)
            .ToList());
    }

    [Fact]
    public void SkipAfterReverseAndDistinctDoesNotSkipTheUnreversedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAfterReverseAndDistinctDoesNotSkipTheUnreversedValues));

        List<string> expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .Skip(1)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H24aReverseDistinctRow>()
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .Skip(1)
            .ToList());
    }

    [Fact]
    public void FirstAfterReverseAndDistinctDoesNotReadTheFirstOrderedValue()
    {
        using TestDatabase db = Setup(nameof(FirstAfterReverseAndDistinctDoesNotReadTheFirstOrderedValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .First();

        AssertMatchesOrIsRefused([expected], () =>
        [
            db.Table<H24aReverseDistinctRow>()
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .Reverse()
                .Distinct()
                .First()
        ]);
    }

    [Fact]
    public void ElementAtAfterReverseAndDistinctDoesNotIndexTheUnreversedValues()
    {
        using TestDatabase db = Setup(nameof(ElementAtAfterReverseAndDistinctDoesNotIndexTheUnreversedValues));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .ElementAt(1);

        AssertMatchesOrIsRefused([expected], () =>
        [
            db.Table<H24aReverseDistinctRow>()
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .Reverse()
                .Distinct()
                .ElementAt(1)
        ]);
    }

    [Fact]
    public void FirstAfterReverseAndDistinctOverWholeRowsDoesNotReadTheFirstOrderedRow()
    {
        using TestDatabase db = Setup(nameof(FirstAfterReverseAndDistinctOverWholeRowsDoesNotReadTheFirstOrderedRow));

        int expected = Rows().OrderBy(r => r.Id).Reverse().Distinct().First().Id;

        AssertMatchesOrIsRefused([expected], () =>
        [
            db.Table<H24aReverseDistinctRow>().OrderBy(r => r.Id).Reverse().Distinct().First().Id
        ]);
    }

    [Fact]
    public void ConcatAfterReverseAndDistinctDoesNotReverseTheWholeCombinedResult()
    {
        using TestDatabase db = Setup(nameof(ConcatAfterReverseAndDistinctDoesNotReverseTheWholeCombinedResult));

        List<int> expected = Rows()
            .Select(r => r.Id)
            .Where(v => v <= 3)
            .Reverse()
            .Distinct()
            .Concat(Rows().Select(r => r.Id).Where(v => v >= 4))
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H24aReverseDistinctRow>()
            .Select(r => r.Id)
            .Where(v => v <= 3)
            .Reverse()
            .Distinct()
            .Concat(db.Table<H24aReverseDistinctRow>().Select(r => r.Id).Where(v => v >= 4))
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

    private static List<H24aReverseDistinctRow> Rows()
    {
        return
        [
            new H24aReverseDistinctRow { Id = 1, Name = "a" },
            new H24aReverseDistinctRow { Id = 2, Name = "b" },
            new H24aReverseDistinctRow { Id = 3, Name = "c" },
            new H24aReverseDistinctRow { Id = 4, Name = "d" },
            new H24aReverseDistinctRow { Id = 5, Name = "e" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24aReverseDistinctRow>().Schema.CreateTable();
        db.Table<H24aReverseDistinctRow>().AddRange(Rows());
        return db;
    }
}
