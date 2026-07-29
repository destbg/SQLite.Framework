using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChoLeftRows")]
public class ChoLeftRow
{
    [Key]
    public int Id { get; set; }
}

[Table("ChoRightRows")]
public class ChoRightRow
{
    [Key]
    public int Id { get; set; }

    public int LeftId { get; set; }
}

public class ChoPair
{
    public ChoLeftRow A { get; set; } = new();

    public ChoRightRow? B { get; set; }
}

public class CteOptionalRowMemberTests
{
    [Fact]
    public void ACteCarryingAnOptionalMemberCountsItsUnmatchedRows()
    {
        using TestDatabase db = Seed();

        SQLiteCte<ChoPair> cte = db.With(() =>
            from a in db.Table<ChoLeftRow>()
            join b in db.Table<ChoRightRow>() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            select new ChoPair { A = a, B = b });

        Assert.Equal(OraclePairs().Count(p => p.B == null), cte.Count(p => p.B == null));
    }

    [Fact]
    public void ACteCarryingAnOptionalMemberJoinsToATable()
    {
        using TestDatabase db = Seed();

        SQLiteCte<ChoPair> cte = db.With(() =>
            from a in db.Table<ChoLeftRow>()
            join b in db.Table<ChoRightRow>() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            select new ChoPair { A = a, B = b });

        List<int> expected = Lefts()
            .Join(OraclePairs(), l => l.Id, p => p.A.Id, (l, p) => p.B != null ? p.B.Id : 0)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<ChoLeftRow>()
            .Join(cte, l => l.Id, p => p.A.Id, (l, p) => p.B != null ? p.B.Id : 0)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ARecursiveCteCarryingAnOptionalMemberCountsItsUnmatchedRows()
    {
        using TestDatabase db = Seed();

        SQLiteCte<ChoPair> cte = RecursivePairs(db);

        Assert.Equal(OraclePairs().Count(p => p.B == null), cte.Count(p => p.B == null));
    }

    [Fact]
    public void ARecursiveCteCarryingAnOptionalMemberJoinsToATable()
    {
        using TestDatabase db = Seed();

        SQLiteCte<ChoPair> cte = RecursivePairs(db);

        List<int> expected = Lefts()
            .Join(OraclePairs(), l => l.Id, p => p.A.Id, (l, p) => p.B != null ? p.B.Id : 0)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<ChoLeftRow>()
            .Join(cte, l => l.Id, p => p.A.Id, (l, p) => p.B != null ? p.B.Id : 0)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static SQLiteCte<ChoPair> RecursivePairs(TestDatabase db)
    {
        return db.WithRecursive<ChoPair>(self =>
            (from a in db.Table<ChoLeftRow>()
             join b in db.Table<ChoRightRow>() on a.Id equals b.LeftId into g
             from b in g.DefaultIfEmpty()
             select new ChoPair { A = a, B = b })
            .Concat(
                from p in self
                join a in db.Table<ChoLeftRow>() on p.A.Id equals a.Id
                join b in db.Table<ChoRightRow>() on a.Id equals b.LeftId into g
                from b in g.DefaultIfEmpty()
                where a.Id != a.Id
                select new ChoPair { A = a, B = b }));
    }

    private static List<ChoLeftRow> Lefts()
    {
        return
        [
            new ChoLeftRow { Id = 1 },
            new ChoLeftRow { Id = 2 },
            new ChoLeftRow { Id = 3 }
        ];
    }

    private static List<ChoRightRow> Rights()
    {
        return
        [
            new ChoRightRow { Id = 1, LeftId = 1 },
            new ChoRightRow { Id = 2, LeftId = 3 }
        ];
    }

    private static List<ChoPair> OraclePairs()
    {
        return (
            from a in Lefts()
            join b in Rights() on a.Id equals b.LeftId into g
            from b in g.DefaultIfEmpty()
            select new ChoPair { A = a, B = b })
            .ToList();
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<ChoLeftRow>().Schema.CreateTable();
        db.Table<ChoRightRow>().Schema.CreateTable();
        db.Table<ChoLeftRow>().AddRange(Lefts());
        db.Table<ChoRightRow>().AddRange(Rights());
        return db;
    }
}
