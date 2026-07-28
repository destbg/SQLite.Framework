using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23cDoubleReverseRows")]
public class H23cDoubleReverseRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23cDoubleReverseText
{
    public static string LastLetter(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class RepeatedReverseAroundClientDistinctTests
{
    [Fact]
    public void ReverseOnBothSidesOfDistinctOverAClientProjectionKeepsTheLastOccurrences()
    {
        using TestDatabase db = Setup(nameof(ReverseOnBothSidesOfDistinctOverAClientProjectionKeepsTheLastOccurrences));
        List<H23cDoubleReverseRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H23cDoubleReverseText.LastLetter(r.Name))
            .Reverse()
            .Distinct()
            .Reverse()
            .ToList();

        List<string> actual = db.Table<H23cDoubleReverseRow>()
            .OrderBy(r => r.Name)
            .Select(r => H23cDoubleReverseText.LastLetter(r.Name))
            .Reverse()
            .Distinct()
            .Reverse()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseBeforeDistinctOverAPlainColumnProjectionReversesTheDistinctValues()
    {
        using TestDatabase db = Setup(nameof(ReverseBeforeDistinctOverAPlainColumnProjectionReversesTheDistinctValues));

        List<string> expected = Rows()
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .ToList();

        List<string> actual = db.Table<H23cDoubleReverseRow>()
            .Select(r => r.Name)
            .Reverse()
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23cDoubleReverseRow> Rows()
    {
        return
        [
            new H23cDoubleReverseRow { Id = 1, Name = "1a" },
            new H23cDoubleReverseRow { Id = 2, Name = "2a" },
            new H23cDoubleReverseRow { Id = 3, Name = "3b" },
            new H23cDoubleReverseRow { Id = 4, Name = "4c" },
            new H23cDoubleReverseRow { Id = 5, Name = "5b" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23cDoubleReverseRow>().Schema.CreateTable();
        db.Table<H23cDoubleReverseRow>().AddRange(Rows());
        return db;
    }
}
