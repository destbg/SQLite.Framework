using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26cReverseDistinctValueRows")]
public class H26cReverseDistinctValueRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class ReverseBeforeDatabaseDistinctOrderTests
{
    [Fact]
    public void DistinctAfterReverseKeepsTheFirstValueOfTheReversedSequence()
    {
        using TestDatabase db = Setup(nameof(DistinctAfterReverseKeepsTheFirstValueOfTheReversedSequence));

        List<int> expected = Rows()
            .Select(r => r.Amount)
            .Reverse()
            .Distinct()
            .ToList();

        List<int> actual = db.Table<H26cReverseDistinctValueRow>()
            .Select(r => r.Amount)
            .Reverse()
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26cReverseDistinctValueRow> Rows()
    {
        return
        [
            new H26cReverseDistinctValueRow { Id = 1, Amount = 3 },
            new H26cReverseDistinctValueRow { Id = 2, Amount = 1 },
            new H26cReverseDistinctValueRow { Id = 3, Amount = 3 },
            new H26cReverseDistinctValueRow { Id = 4, Amount = 2 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26cReverseDistinctValueRow>().Schema.CreateTable();
        db.Table<H26cReverseDistinctValueRow>().AddRange(Rows());
        return db;
    }
}
