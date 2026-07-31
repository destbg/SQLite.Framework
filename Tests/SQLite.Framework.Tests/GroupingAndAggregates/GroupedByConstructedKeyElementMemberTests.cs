using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupedByConstructedKeyElementMemberTests
{
    [Fact]
    public void GroupingByAConstructedNestedKeySumsTheElementDefaults()
    {
        using TestDatabase db = Setup(nameof(GroupingByAConstructedNestedKeySumsTheElementDefaults));

        List<int> actual = db.Table<H26aGroupedRow>()
            .Select(r => new H26aGroupedOuter { K = r.K, Part = new H26aGroupedPart { P = r.A } })
            .GroupBy(x => new { W = new H26aGroupedPart { P = x.K } })
            .Select(g => g.Sum(e => e.Part!.Q))
            .AsEnumerable()
            .ToList();

        Assert.Equal(new List<int> { 0 }, actual);
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aGroupedRow>().Schema.CreateTable();
        db.Table<H26aGroupedRow>().AddRange(
        [
            new H26aGroupedRow { Id = 1, K = 7, A = 5 },
            new H26aGroupedRow { Id = 2, K = 7, A = 20 }
        ]);
        return db;
    }
}
