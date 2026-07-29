using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24aTwoDistinctRows")]
public class H24aTwoDistinctRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H24aTwoDistinctText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class ReverseBetweenTwoDistinctsClientProjectionOrderTests
{
    [Fact]
    public void ReverseBetweenTwoDistinctsOverAClientProjectionReversesTheDedupedValues()
    {
        using TestDatabase db = Setup(nameof(ReverseBetweenTwoDistinctsOverAClientProjectionReversesTheDedupedValues));

        List<string> expected = Rows()
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => H24aTwoDistinctText.Tail(r.Name))
            .Distinct()
            .Reverse()
            .Distinct()
            .ToList();

        List<string> actual = db.Table<H24aTwoDistinctRow>()
            .OrderBy(r => r.Name)
            .Select(r => H24aTwoDistinctText.Tail(r.Name))
            .Distinct()
            .Reverse()
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24aTwoDistinctRow> Rows()
    {
        return
        [
            new H24aTwoDistinctRow { Id = 1, Name = "1a" },
            new H24aTwoDistinctRow { Id = 2, Name = "2b" },
            new H24aTwoDistinctRow { Id = 3, Name = "3a" },
            new H24aTwoDistinctRow { Id = 4, Name = "4c" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24aTwoDistinctRow>().Schema.CreateTable();
        db.Table<H24aTwoDistinctRow>().AddRange(Rows());
        return db;
    }
}
