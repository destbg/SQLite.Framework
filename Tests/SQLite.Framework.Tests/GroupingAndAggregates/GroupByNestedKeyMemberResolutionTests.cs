using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NestedKeyGroupRows")]
public class NestedKeyGroupRow
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }
}

public class GroupByNestedKeyMemberResolutionTests
{
    private static List<NestedKeyGroupRow> Rows()
    {
        return
        [
            new NestedKeyGroupRow { Id = 1, Bucket = 1 },
            new NestedKeyGroupRow { Id = 3, Bucket = 1 },
            new NestedKeyGroupRow { Id = 2, Bucket = 2 },
            new NestedKeyGroupRow { Id = 4, Bucket = 2 },
            new NestedKeyGroupRow { Id = 6, Bucket = 2 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NestedKeyGroupRow>().Schema.CreateTable();
        db.Table<NestedKeyGroupRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void OrderByNestedKeyMemberUsesTheNestedColumn()
    {
        using TestDatabase db = Seed();
        List<NestedKeyGroupRow> rows = Rows();

        List<(int, int, int)> expected = rows
            .GroupBy(r => new { r.Bucket, Inner = new { Mod = r.Id % 2 } })
            .OrderBy(g => g.Key.Inner.Mod)
            .Select(g => (g.Key.Bucket, g.Key.Inner.Mod, g.Count()))
            .ToList();
        List<(int, int, int)> actual = db.Table<NestedKeyGroupRow>()
            .GroupBy(r => new { r.Bucket, Inner = new { Mod = r.Id % 2 } })
            .OrderBy(g => g.Key.Inner.Mod)
            .Select(g => new { g.Key.Bucket, g.Key.Inner.Mod, C = g.Count() })
            .ToList()
            .Select(x => (x.Bucket, x.Mod, x.C))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HavingOnNestedKeyMemberUsesTheNestedColumn()
    {
        using TestDatabase db = Seed();
        List<NestedKeyGroupRow> rows = Rows();

        List<int> expected = rows
            .GroupBy(r => new { r.Bucket, Inner = new { Mod = r.Id % 2 } })
            .Where(g => g.Key.Inner.Mod == 0)
            .Select(g => g.Key.Bucket)
            .OrderBy(b => b)
            .ToList();
        List<int> actual = db.Table<NestedKeyGroupRow>()
            .GroupBy(r => new { r.Bucket, Inner = new { Mod = r.Id % 2 } })
            .Where(g => g.Key.Inner.Mod == 0)
            .Select(g => g.Key.Bucket)
            .ToList()
            .OrderBy(b => b)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByThreeLevelNestedKeyMemberUsesTheNestedColumn()
    {
        using TestDatabase db = Seed();
        List<NestedKeyGroupRow> rows = Rows();

        List<(int, int)> expected = rows
            .GroupBy(r => new { r.Bucket, Inner = new { Deep = new { Mod = r.Id % 2 } } })
            .OrderBy(g => g.Key.Inner.Deep.Mod)
            .Select(g => (g.Key.Bucket, g.Count()))
            .ToList();
        List<(int, int)> actual = db.Table<NestedKeyGroupRow>()
            .GroupBy(r => new { r.Bucket, Inner = new { Deep = new { Mod = r.Id % 2 } } })
            .OrderBy(g => g.Key.Inner.Deep.Mod)
            .Select(g => new { g.Key.Bucket, C = g.Count() })
            .ToList()
            .Select(x => (x.Bucket, x.C))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
