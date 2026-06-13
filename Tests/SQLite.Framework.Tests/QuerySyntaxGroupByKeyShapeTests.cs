using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QuerySyntaxGroupByKeyShapeTests
{
    private static List<ShelfSlot> Rows()
    {
        return new List<ShelfSlot>
        {
            new ShelfSlot { Id = 1, Label = "a", Rank = 1 },
            new ShelfSlot { Id = 2, Label = "b", Rank = 2 },
            new ShelfSlot { Id = 3, Label = "a", Rank = 3 },
            new ShelfSlot { Id = 4, Label = "b", Rank = 5 },
            new ShelfSlot { Id = 5, Label = "c", Rank = 5 },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<ShelfSlot>().Schema.CreateTable();
        db.Table<ShelfSlot>().AddRange(Rows());
        return db;
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void MemberKeyGroupsRows()
    {
        using TestDatabase db = Seed();

        Dictionary<string, List<int>> oracle = Rows()
            .ToLookup(r => r.Label)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        var groups = (
            from r in db.Table<ShelfSlot>()
            group r by r.Label
        ).ToList();

        Dictionary<string, List<int>> actual = groups
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        Assert.Equal(oracle.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (string key in oracle.Keys)
        {
            Assert.Equal(oracle[key], actual[key]);
        }
    }

    [Fact]
    public void AnonymousKeyGroupsRows()
    {
        using TestDatabase db = Seed();

        var oracle = Rows()
            .ToLookup(r => new { r.Label, r.Rank })
            .Select(g => (g.Key.Label, g.Key.Rank, Ids: string.Join(",", g.Select(x => x.Id).OrderBy(x => x))))
            .OrderBy(t => t.Label).ThenBy(t => t.Rank)
            .ToList();

        var groups = (
            from r in db.Table<ShelfSlot>()
            group r by new { r.Label, r.Rank }
        ).ToList();

        var actual = groups
            .Select(g => (g.Key.Label, g.Key.Rank, Ids: string.Join(",", g.Select(x => x.Id).OrderBy(x => x))))
            .OrderBy(t => t.Label).ThenBy(t => t.Rank)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ComputedKeyGroupsRows()
    {
        using TestDatabase db = Seed();

        Dictionary<int, List<int>> oracle = Rows()
            .ToLookup(r => r.Rank % 2)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        var groups = (
            from r in db.Table<ShelfSlot>()
            group r by r.Rank % 2
        ).ToList();

        Dictionary<int, List<int>> actual = groups
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        Assert.Equal(oracle.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (int key in oracle.Keys)
        {
            Assert.Equal(oracle[key], actual[key]);
        }
    }

    [Fact]
    public void MemberKeyAfterWhereGroupsRows()
    {
        using TestDatabase db = Seed();

        Dictionary<string, List<int>> oracle = Rows()
            .Where(r => r.Rank > 1)
            .ToLookup(r => r.Label)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        var groups = (
            from r in db.Table<ShelfSlot>()
            where r.Rank > 1
            group r by r.Label
        ).ToList();

        Dictionary<string, List<int>> actual = groups
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).OrderBy(x => x).ToList());

        Assert.Equal(oracle.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (string key in oracle.Keys)
        {
            Assert.Equal(oracle[key], actual[key]);
        }
    }
#endif
}

public class ShelfSlot
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Rank { get; set; }
}
