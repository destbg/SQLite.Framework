using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByKeyExpressionShapeTests
{
    private static List<GadgetRow> Rows()
    {
        return new List<GadgetRow>
        {
            new GadgetRow { Id = 1, Value = 1, Flag = true, Status = GadgetStatus.Active },
            new GadgetRow { Id = 2, Value = 2, Flag = false, Status = GadgetStatus.Idle },
            new GadgetRow { Id = 3, Value = 3, Flag = true, Status = GadgetStatus.Active },
            new GadgetRow { Id = 4, Value = 4, Flag = false, Status = GadgetStatus.Broken },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<GadgetRow>().Schema.CreateTable();
        db.Table<GadgetRow>().AddRange(Rows());
        return db;
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void EnumConstantComparisonKeyGroupsRows()
    {
        using TestDatabase db = Seed();

        Dictionary<bool, List<int>> oracle = Rows()
            .ToLookup(r => r.Status == GadgetStatus.Active)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        var groups = db.Table<GadgetRow>()
            .GroupBy(r => r.Status == GadgetStatus.Active)
            .ToList();

        Dictionary<bool, List<int>> actual = groups
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        Assert.Equal(oracle.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (bool key in oracle.Keys)
        {
            Assert.Equal(oracle[key], actual[key]);
        }
    }

    [Fact]
    public void CapturedLocalComparisonKeyGroupsRows()
    {
        using TestDatabase db = Seed();
        int limit = 2;

        Dictionary<bool, List<int>> oracle = Rows()
            .ToLookup(r => r.Value > limit)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        var groups = db.Table<GadgetRow>()
            .GroupBy(r => r.Value > limit)
            .ToList();

        Dictionary<bool, List<int>> actual = groups
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        Assert.Equal(oracle.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (bool key in oracle.Keys)
        {
            Assert.Equal(oracle[key], actual[key]);
        }
    }

    [Fact]
    public void AnonymousKeyWithCapturedLocalGroupsRows()
    {
        using TestDatabase db = Seed();
        int limit = 2;

        var oracle = Rows()
            .ToLookup(r => new { r.Flag, Above = r.Value > limit })
            .Select(g => (g.Key.Flag, g.Key.Above, Ids: string.Join(",", g.Select(x => x.Id).OrderBy(x => x))))
            .OrderBy(t => t.Flag).ThenBy(t => t.Above)
            .ToList();

        var groups = db.Table<GadgetRow>()
            .GroupBy(r => new { r.Flag, Above = r.Value > limit })
            .ToList();

        var actual = groups
            .Select(g => (g.Key.Flag, g.Key.Above, Ids: string.Join(",", g.Select(x => x.Id).OrderBy(x => x))))
            .OrderBy(t => t.Flag).ThenBy(t => t.Above)
            .ToList();

        Assert.Equal(oracle, actual);
    }
#endif
}

public class GadgetRow
{
    public int Id { get; set; }

    public int Value { get; set; }

    public bool Flag { get; set; }

    public GadgetStatus Status { get; set; }
}

public enum GadgetStatus
{
    Idle = 0,
    Active = 1,
    Broken = 2,
}
