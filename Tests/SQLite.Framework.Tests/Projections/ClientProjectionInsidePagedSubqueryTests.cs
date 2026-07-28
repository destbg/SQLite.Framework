using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23zPagedProjectionRows")]
public class H23zPagedProjectionRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23zPagedProjectionText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientProjectionInsidePagedSubqueryTests
{
    [Fact]
    public void TakeBeforeAClientProjectionKeepsTheProjectionWhenDistinctFollows()
    {
        using TestDatabase db = Setup(nameof(TakeBeforeAClientProjectionKeepsTheProjectionWhenDistinctFollows));

        List<string> expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .ToList();

        List<string> actual = db.Table<H23zPagedProjectionRow>()
            .OrderBy(r => r.Name)
            .Take(3)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipBeforeAClientProjectionKeepsTheProjectionWhenDistinctFollows()
    {
        using TestDatabase db = Setup(nameof(SkipBeforeAClientProjectionKeepsTheProjectionWhenDistinctFollows));

        List<string> expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .ToList();

        List<string> actual = db.Table<H23zPagedProjectionRow>()
            .OrderBy(r => r.Name)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstAfterAPagedClientProjectionReadsTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(FirstAfterAPagedClientProjectionReadsTheProjectedValue));

        string expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .First();

        string actual = db.Table<H23zPagedProjectionRow>()
            .OrderBy(r => r.Name)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterAPagedClientProjectionCountsTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterAPagedClientProjectionCountsTheProjectedValues));

        int expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .Count();

        int actual = db.Table<H23zPagedProjectionRow>()
            .OrderBy(r => r.Name)
            .Skip(1)
            .Select(r => H23zPagedProjectionText.Tail(r.Name))
            .Distinct()
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PagedClientProjectionIntoAnonymousTypeReadsTheProjectedMember()
    {
        using TestDatabase db = Setup(nameof(PagedClientProjectionIntoAnonymousTypeReadsTheProjectedMember));

        var expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Skip(1)
            .Select(r => new { T = H23zPagedProjectionText.Tail(r.Name) })
            .Distinct()
            .ToList();

        var actual = db.Table<H23zPagedProjectionRow>()
            .OrderBy(r => r.Name)
            .Skip(1)
            .Select(r => new { T = H23zPagedProjectionText.Tail(r.Name) })
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23zPagedProjectionRow> Rows()
    {
        return
        [
            new H23zPagedProjectionRow { Id = 1, Name = "1a" },
            new H23zPagedProjectionRow { Id = 2, Name = "2a" },
            new H23zPagedProjectionRow { Id = 3, Name = "3b" },
            new H23zPagedProjectionRow { Id = 4, Name = "4a" },
            new H23zPagedProjectionRow { Id = 5, Name = "5c" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23zPagedProjectionRow>().Schema.CreateTable();
        db.Table<H23zPagedProjectionRow>().AddRange(Rows());
        return db;
    }
}
