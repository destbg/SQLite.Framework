using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22rResidualRows")]
public class H22rResidualRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22rResidualText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientProjectionDistinctResidualOperatorTests
{
    [Fact]
    public void ReverseAfterDistinctOverAClientProjectionReversesTheDistinctValues()
    {
        using TestDatabase db = Setup(nameof(ReverseAfterDistinctOverAClientProjectionReversesTheDistinctValues));
        List<H22rResidualRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Reverse()
            .ToList();

        List<string> actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Reverse()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipAfterDistinctOverAClientProjectionSkipsDistinctValues()
    {
        using TestDatabase db = Setup(nameof(SkipAfterDistinctOverAClientProjectionSkipsDistinctValues));
        List<H22rResidualRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Skip(1)
            .ToList();

        List<string> actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Skip(1)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipAfterTakeAfterDistinctOverAClientProjectionAdjustsTheWindow()
    {
        using TestDatabase db = Setup(nameof(SkipAfterTakeAfterDistinctOverAClientProjectionAdjustsTheWindow));
        List<H22rResidualRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .Skip(1)
            .ToList();

        List<string> actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .Skip(1)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstAfterDistinctOverAClientProjectionReturnsTheFirstDistinctValue()
    {
        using TestDatabase db = Setup(nameof(FirstAfterDistinctOverAClientProjectionReturnsTheFirstDistinctValue));
        List<H22rResidualRow> local = Rows();

        string expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .First();

        string actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstAfterTakeAfterDistinctOverAClientProjectionKeepsTheSmallerWindow()
    {
        using TestDatabase db = Setup(nameof(FirstAfterTakeAfterDistinctOverAClientProjectionKeepsTheSmallerWindow));
        List<H22rResidualRow> local = Rows();

        string expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .First();

        string actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleAfterDistinctOverAClientProjectionThrowsOnTwoDistinctValues()
    {
        using TestDatabase db = Setup(nameof(SingleAfterDistinctOverAClientProjectionThrowsOnTwoDistinctValues));

        Assert.Throws<InvalidOperationException>(() => db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Single());
    }

    [Fact]
    public void SingleAfterTakeAfterDistinctOverAClientProjectionSeesOneValue()
    {
        using TestDatabase db = Setup(nameof(SingleAfterTakeAfterDistinctOverAClientProjectionSeesOneValue));
        List<H22rResidualRow> local = Rows();

        string expected = local
            .Where(r => r.Name != "3b" && r.Name != "4c")
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(5)
            .Single();

        string actual = db.Table<H22rResidualRow>()
            .Where(r => r.Name != "3b" && r.Name != "4c")
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(5)
            .Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtAfterDistinctOverAClientProjectionIndexesDistinctValues()
    {
        using TestDatabase db = Setup(nameof(ElementAtAfterDistinctOverAClientProjectionIndexesDistinctValues));
        List<H22rResidualRow> local = Rows();

        string expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .ElementAt(1);

        string actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .ElementAt(1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtAfterTakeAfterDistinctOverAClientProjectionKeepsTheWindow()
    {
        using TestDatabase db = Setup(nameof(ElementAtAfterTakeAfterDistinctOverAClientProjectionKeepsTheWindow));
        List<H22rResidualRow> local = Rows();

        string expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ElementAt(1);

        string actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ElementAt(1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefaultWithNegativeIndexAfterDistinctOverAClientProjectionReturnsDefault()
    {
        using TestDatabase db = Setup(nameof(ElementAtOrDefaultWithNegativeIndexAfterDistinctOverAClientProjectionReturnsDefault));

        string? actual = db.Table<H22rResidualRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22rResidualText.Tail(r.Name))
            .Distinct()
            .ElementAtOrDefault(-1);

        Assert.Null(actual);
    }

    private static List<H22rResidualRow> Rows()
    {
        return
        [
            new H22rResidualRow { Id = 1, Name = "1a" },
            new H22rResidualRow { Id = 2, Name = "2a" },
            new H22rResidualRow { Id = 3, Name = "3b" },
            new H22rResidualRow { Id = 4, Name = "4c" },
            new H22rResidualRow { Id = 5, Name = "5a" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22rResidualRow>().Schema.CreateTable();
        db.Table<H22rResidualRow>().AddRange(Rows());
        return db;
    }
}
