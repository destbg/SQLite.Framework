using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GoseLeftRows")]
public class GoseLeftRow
{
    [Key]
    public int Id { get; set; }

    public int Kind { get; set; }
}

[Table("GoseRightRows")]
public class GoseRightRow
{
    [Key]
    public int Id { get; set; }

    public int LeftId { get; set; }

    public int Value { get; set; }
}

public class GroupByOptionalScalarElementTests
{
    [Fact]
    public void GroupingAnOptionalScalarElementCountsLikeLinqToObjects()
    {
        using TestDatabase db = Seed();

        List<int> expected = (
            from a in Lefts()
            join b in Rights() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            select new { a, b })
            .GroupBy(x => x.a.Kind, x => x.b != null ? x.b.Value : 0)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = (
            from a in db.Table<GoseLeftRow>()
            join b in db.Table<GoseRightRow>() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            select new { a, b })
            .GroupBy(x => x.a.Kind, x => x.b != null ? x.b.Value : 0)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupingAnOptionalMemberValueElementCountsLikeLinqToObjects()
    {
        using TestDatabase db = Seed();

        List<int> expected = (
            from a in Lefts()
            join b in Rights() on a.Id equals b.LeftId
            select new { a, b })
            .GroupBy(x => x.a.Kind, x => x.b.Value)
            .Select(g => g.Where(v => v >= 0).Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = (
            from a in db.Table<GoseLeftRow>()
            join b in db.Table<GoseRightRow>() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            where b != null
            select new { a, b })
            .GroupBy(x => x.a.Kind, x => x.b!.Value)
            .Select(g => g.Where(v => v >= 0).Count())
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<GoseLeftRow> Lefts()
    {
        return
        [
            new GoseLeftRow { Id = 1, Kind = 1 },
            new GoseLeftRow { Id = 2, Kind = 1 },
            new GoseLeftRow { Id = 3, Kind = 2 }
        ];
    }

    private static List<GoseRightRow> Rights()
    {
        return
        [
            new GoseRightRow { Id = 1, LeftId = 1, Value = 10 },
            new GoseRightRow { Id = 2, LeftId = 3, Value = 30 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<GoseLeftRow>().Schema.CreateTable();
        db.Table<GoseRightRow>().Schema.CreateTable();
        db.Table<GoseLeftRow>().AddRange(Lefts());
        db.Table<GoseRightRow>().AddRange(Rights());
        return db;
    }
}
