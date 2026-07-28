using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23zChainedOperatorRows")]
public class H23zChainedOperatorRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23zChainedOperatorText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientProjectionChainedOperatorValueTests
{
    [Fact]
    public void SecondSelectAfterAClientProjectionReadsTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(SecondSelectAfterAClientProjectionReadsTheProjectedValue));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Select(v => v.Length)
            .ToList();

        List<int> actual = db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Select(v => v.Length)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectAfterDistinctOverAClientProjectionReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(SelectAfterDistinctOverAClientProjectionReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Distinct()
            .Select(v => v + "!")
            .ToList());

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void WhereAfterDistinctOverAClientProjectionReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(WhereAfterDistinctOverAClientProjectionReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Take(3)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Distinct()
            .Where(v => v == "a")
            .ToList());

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void GroupByOverAPagedClientProjectionReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(GroupByOverAPagedClientProjectionReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Take(3)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .GroupBy(v => v)
            .Select(g => g.Count())
            .ToList());

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void UnionWithAClientProjectionReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(UnionWithAClientProjectionReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Union(db.Table<H23zChainedOperatorRow>().Select(r => r.Name))
            .ToList());

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void ElementAtOverAClientProjectionReadsTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(ElementAtOverAClientProjectionReadsTheProjectedValue));

        string expected = Rows().OrderBy(r => r.Id).Select(r => H23zChainedOperatorText.Tail(r.Name)).ElementAt(2);
        string actual = db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .ElementAt(2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefaultBeyondTheEndOfAClientProjectionReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(ElementAtOrDefaultBeyondTheEndOfAClientProjectionReturnsTheDefault));

        string? actual = db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .ElementAtOrDefault(10);

        Assert.Null(actual);
    }

    [Fact]
    public void FirstOrDefaultOverAnEmptyClientProjectionReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(FirstOrDefaultOverAnEmptyClientProjectionReturnsTheDefault));

        string? actual = db.Table<H23zChainedOperatorRow>()
            .Where(r => r.Id > 99)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .FirstOrDefault();

        Assert.Null(actual);
    }

    [Fact]
    public void SingleOrDefaultOverAFilteredClientProjectionReadsTheValue()
    {
        using TestDatabase db = Setup(nameof(SingleOrDefaultOverAFilteredClientProjectionReadsTheValue));

        string? expected = Rows().Where(r => r.Id == 2).Select(r => H23zChainedOperatorText.Tail(r.Name)).SingleOrDefault();
        string? actual = db.Table<H23zChainedOperatorRow>()
            .Where(r => r.Id == 2)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .SingleOrDefault();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountOverAClientProjectionCountsTheRows()
    {
        using TestDatabase db = Setup(nameof(LongCountOverAClientProjectionCountsTheRows));

        long expected = Rows().Select(r => H23zChainedOperatorText.Tail(r.Name)).LongCount();
        long actual = db.Table<H23zChainedOperatorRow>()
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyOverAPagedDistinctClientProjectionReportsWhetherRowsExist()
    {
        using TestDatabase db = Setup(nameof(AnyOverAPagedDistinctClientProjectionReportsWhetherRowsExist));

        bool expected = Rows().OrderBy(r => r.Id).Take(3).Select(r => H23zChainedOperatorText.Tail(r.Name)).Distinct().Any();
        bool actual = db.Table<H23zChainedOperatorRow>()
            .OrderBy(r => r.Id)
            .Take(3)
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Distinct()
            .Any();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyWithAPredicateOverADistinctClientProjectionReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(AnyWithAPredicateOverADistinctClientProjectionReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Distinct()
            .Any(v => v == "a"));

        Assert.Contains("Unsupported WHERE expression", error.Message);
    }

    [Fact]
    public void SecondSelectIgnoringTheProjectedValueReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(SecondSelectIgnoringTheProjectedValueReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zChainedOperatorRow>()
            .Select(r => H23zChainedOperatorText.Tail(r.Name))
            .Select(v => 5)
            .ToList());

        Assert.Contains("A second Select after a projection that runs in memory", error.Message);
    }

    private static List<H23zChainedOperatorRow> Rows()
    {
        return
        [
            new H23zChainedOperatorRow { Id = 1, Name = "1a" },
            new H23zChainedOperatorRow { Id = 2, Name = "2a" },
            new H23zChainedOperatorRow { Id = 3, Name = "3b" },
            new H23zChainedOperatorRow { Id = 4, Name = "4a" },
            new H23zChainedOperatorRow { Id = 5, Name = "5c" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23zChainedOperatorRow>().Schema.CreateTable();
        db.Table<H23zChainedOperatorRow>().AddRange(Rows());
        return db;
    }
}
