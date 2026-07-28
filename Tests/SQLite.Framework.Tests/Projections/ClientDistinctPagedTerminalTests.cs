using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23zPagedTerminalRows")]
public class H23zPagedTerminalRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23zPagedTerminalText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientDistinctPagedTerminalTests
{
    [Fact]
    public void CountAfterTakeOverAClientDistinctCountsThePagedValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterTakeOverAClientDistinctCountsThePagedValues));

        int expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(1)
            .Count();

        int actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(1)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterSkipOverAClientDistinctCountsTheRemainingValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterSkipOverAClientDistinctCountsTheRemainingValues));

        int expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Skip(1)
            .Count();

        int actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Skip(1)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountAfterTakeOverAClientDistinctCountsThePagedValues()
    {
        using TestDatabase db = Setup(nameof(LongCountAfterTakeOverAClientDistinctCountsThePagedValues));

        long expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .LongCount();

        long actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyAfterTakeZeroOverAClientDistinctIsFalse()
    {
        using TestDatabase db = Setup(nameof(AnyAfterTakeZeroOverAClientDistinctIsFalse));

        bool expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(0)
            .Any();

        bool actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(0)
            .Any();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyAfterSkipPastTheEndOverAClientDistinctIsFalse()
    {
        using TestDatabase db = Setup(nameof(AnyAfterSkipPastTheEndOverAClientDistinctIsFalse));

        bool expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Skip(10)
            .Any();

        bool actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Skip(10)
            .Any();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtAfterTakeOverAClientDistinctReadsTheValue()
    {
        using TestDatabase db = Setup(nameof(ElementAtAfterTakeOverAClientDistinctReadsTheValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ElementAt(1);

        string actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ElementAt(1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefaultAfterTakeOverAClientDistinctBeyondTheEndReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(ElementAtOrDefaultAfterTakeOverAClientDistinctBeyondTheEndReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(1)
            .ElementAtOrDefault(5);

        Assert.Null(actual);
    }

    [Fact]
    public void FirstAfterTakeOverAClientDistinctReadsTheFirstValue()
    {
        using TestDatabase db = Setup(nameof(FirstAfterTakeOverAClientDistinctReadsTheFirstValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .First();

        string actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultAfterTakeZeroOverAClientDistinctReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(FirstOrDefaultAfterTakeZeroOverAClientDistinctReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(0)
            .FirstOrDefault();

        Assert.Null(actual);
    }

    [Fact]
    public void SingleAfterTakeOneOverAClientDistinctReadsTheOnlyValue()
    {
        using TestDatabase db = Setup(nameof(SingleAfterTakeOneOverAClientDistinctReadsTheOnlyValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(1)
            .Single();

        string actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(1)
            .Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrDefaultAfterTakeZeroOverAClientDistinctReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(SingleOrDefaultAfterTakeZeroOverAClientDistinctReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Take(0)
            .SingleOrDefault();

        Assert.Null(actual);
    }

    [Fact]
    public void ElementAtOverADistinctPagedSourceReadsTheValue()
    {
        using TestDatabase db = Setup(nameof(ElementAtOverADistinctPagedSourceReadsTheValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .ElementAt(1);

        string actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .ElementAt(1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefaultOverADistinctPagedSourceBeyondTheEndReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(ElementAtOrDefaultOverADistinctPagedSourceBeyondTheEndReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .ElementAtOrDefault(9);

        Assert.Null(actual);
    }

    [Fact]
    public void FirstOverADistinctPagedSourceReadsTheFirstValue()
    {
        using TestDatabase db = Setup(nameof(FirstOverADistinctPagedSourceReadsTheFirstValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .First();

        string actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultOverAnEmptyDistinctPagedSourceReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(FirstOrDefaultOverAnEmptyDistinctPagedSourceReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .Where(r => r.Id > 99)
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .FirstOrDefault();

        Assert.Null(actual);
    }

    [Fact]
    public void SingleOverAOneRowDistinctPagedSourceReadsTheOnlyValue()
    {
        using TestDatabase db = Setup(nameof(SingleOverAOneRowDistinctPagedSourceReadsTheOnlyValue));

        string expected = Rows()
            .Where(r => r.Id == 2)
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Single();

        string actual = db.Table<H23zPagedTerminalRow>()
            .Where(r => r.Id == 2)
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrDefaultOverAnEmptyDistinctPagedSourceReturnsTheDefault()
    {
        using TestDatabase db = Setup(nameof(SingleOrDefaultOverAnEmptyDistinctPagedSourceReturnsTheDefault));

        string? actual = db.Table<H23zPagedTerminalRow>()
            .Where(r => r.Id > 99)
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .SingleOrDefault();

        Assert.Null(actual);
    }

    [Fact]
    public void FirstWithAPredicateOverADistinctPagedSourceReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(FirstWithAPredicateOverADistinctPagedSourceReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .First(v => v == "a"));

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void LongCountOverADistinctPagedSourceCountsTheValues()
    {
        using TestDatabase db = Setup(nameof(LongCountOverADistinctPagedSourceCountsTheValues));

        long expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .LongCount();

        long actual = db.Table<H23zPagedTerminalRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedTerminalText.Tail(r.Name))
            .Distinct()
            .LongCount();

        Assert.Equal(expected, actual);
    }

    private static List<H23zPagedTerminalRow> Rows()
    {
        return
        [
            new H23zPagedTerminalRow { Id = 1, Name = "1a" },
            new H23zPagedTerminalRow { Id = 2, Name = "2a" },
            new H23zPagedTerminalRow { Id = 3, Name = "3b" },
            new H23zPagedTerminalRow { Id = 4, Name = "4a" },
            new H23zPagedTerminalRow { Id = 5, Name = "5c" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23zPagedTerminalRow>().Schema.CreateTable();
        db.Table<H23zPagedTerminalRow>().AddRange(Rows());
        return db;
    }
}
