using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22hDistinctOrderRows")]
public class H22hDistinctOrderRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22hProjectionText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientProjectionDistinctOperatorOrderTests
{
    [Fact]
    public void TakeAfterDistinctOverAClientProjectionCountsProjectedValues()
    {
        using TestDatabase db = Setup(nameof(TakeAfterDistinctOverAClientProjectionCountsProjectedValues));
        List<H22hDistinctOrderRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ToList();

        List<string> actual = db.Table<H22hDistinctOrderRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Distinct()
            .Take(2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctAfterReverseOverAClientProjectionKeepsTheReversedFirstOccurrences()
    {
        using TestDatabase db = Setup(nameof(DistinctAfterReverseOverAClientProjectionKeepsTheReversedFirstOccurrences));
        List<H22hDistinctOrderRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Reverse()
            .Distinct()
            .ToList();

        List<string> actual = db.Table<H22hDistinctOrderRow>()
            .OrderBy(r => r.Name)
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Reverse()
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleAfterDistinctOverAClientProjectionSeesOneProjectedValue()
    {
        using TestDatabase db = Setup(nameof(SingleAfterDistinctOverAClientProjectionSeesOneProjectedValue));
        List<H22hDistinctOrderRow> local = Rows();

        string expected = local
            .Where(r => r.Name != "3b")
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Distinct()
            .Single();

        string actual = db.Table<H22hDistinctOrderRow>()
            .Where(r => r.Name != "3b")
            .Select(r => H22hProjectionText.Tail(r.Name))
            .Distinct()
            .Single();

        Assert.Equal(expected, actual);
    }

    private static List<H22hDistinctOrderRow> Rows()
    {
        return
        [
            new H22hDistinctOrderRow { Id = 1, Name = "1a" },
            new H22hDistinctOrderRow { Id = 2, Name = "2a" },
            new H22hDistinctOrderRow { Id = 3, Name = "3b" },
            new H22hDistinctOrderRow { Id = 4, Name = "4a" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22hDistinctOrderRow>().Schema.CreateTable();
        db.Table<H22hDistinctOrderRow>().AddRange(Rows());
        return db;
    }
}
