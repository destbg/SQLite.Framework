using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CoalesceLiftedOrderRow
{
    [Key]
    public int Id { get; set; }

    public bool? Flag { get; set; }

    public int? Num { get; set; }
}

public class CoalesceLiftedOrderComparisonNullOperandTests
{
    private static readonly CoalesceLiftedOrderRow[] Data =
    [
        new CoalesceLiftedOrderRow { Id = 1, Flag = null, Num = null },
        new CoalesceLiftedOrderRow { Id = 2, Flag = null, Num = 10 },
        new CoalesceLiftedOrderRow { Id = 3, Flag = null, Num = 3 },
        new CoalesceLiftedOrderRow { Id = 4, Flag = true, Num = null },
        new CoalesceLiftedOrderRow { Id = 5, Flag = false, Num = null },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<CoalesceLiftedOrderRow>().Schema.CreateTable();
        foreach (CoalesceLiftedOrderRow r in Data)
        {
            db.Table<CoalesceLiftedOrderRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void CoalesceRightOperandLiftedComparison()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Data
            .Where(x => (x.Flag ?? (x.Num > 5)) == false)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<CoalesceLiftedOrderRow>()
            .Where(x => (x.Flag ?? (x.Num > 5)) == false)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3, 5], expected);
        Assert.Equal(expected, actual);
    }
}
