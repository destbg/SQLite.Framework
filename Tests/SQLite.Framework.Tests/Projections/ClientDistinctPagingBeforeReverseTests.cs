using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23cReverseOrderRows")]
public class H23cReverseOrderRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23cReverseOrderText
{
    public static string LastLetter(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ClientDistinctPagingBeforeReverseTests
{
    [Fact]
    public void SkipAfterDistinctOverAClientProjectionRunsBeforeReverse()
    {
        using TestDatabase db = Setup(nameof(SkipAfterDistinctOverAClientProjectionRunsBeforeReverse));
        List<H23cReverseOrderRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23cReverseOrderText.LastLetter(r.Name))
            .Distinct()
            .Skip(1)
            .Reverse()
            .ToList();

        List<string> actual = db.Table<H23cReverseOrderRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23cReverseOrderText.LastLetter(r.Name))
            .Distinct()
            .Skip(1)
            .Reverse()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeAfterDistinctOverAClientProjectionRunsBeforeReverse()
    {
        using TestDatabase db = Setup(nameof(TakeAfterDistinctOverAClientProjectionRunsBeforeReverse));
        List<H23cReverseOrderRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23cReverseOrderText.LastLetter(r.Name))
            .Distinct()
            .Take(2)
            .Reverse()
            .ToList();

        List<string> actual = db.Table<H23cReverseOrderRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23cReverseOrderText.LastLetter(r.Name))
            .Distinct()
            .Take(2)
            .Reverse()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23cReverseOrderRow> Rows()
    {
        return
        [
            new H23cReverseOrderRow { Id = 1, Name = "1a" },
            new H23cReverseOrderRow { Id = 2, Name = "2a" },
            new H23cReverseOrderRow { Id = 3, Name = "3b" },
            new H23cReverseOrderRow { Id = 4, Name = "4c" },
            new H23cReverseOrderRow { Id = 5, Name = "5b" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23cReverseOrderRow>().Schema.CreateTable();
        db.Table<H23cReverseOrderRow>().AddRange(Rows());
        return db;
    }
}
