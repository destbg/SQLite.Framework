using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21eLjOwners")]
public class H21eLjOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H21eLjNotes")]
public class H21eLjNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Label { get; set; } = "";
}

public class H21eLjPair
{
    public int Ref { get; set; }

    public string Label { get; set; } = "";
}

public class LeftJoinProjectedRowNullCheckParityTests
{
    private static List<H21eLjOwner> Owners()
    {
        return
        [
            new H21eLjOwner { Id = 1, Name = "Ann" },
            new H21eLjOwner { Id = 2, Name = "Bob" },
            new H21eLjOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H21eLjNote> Notes()
    {
        return
        [
            new H21eLjNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H21eLjNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21eLjOwner>().Schema.CreateTable();
        db.Table<H21eLjNote>().Schema.CreateTable();
        db.Table<H21eLjOwner>().AddRange(Owners());
        db.Table<H21eLjNote>().AddRange(Notes());
        return db;
    }

    [Fact]
    public void OrphanProjectedRowTakesTheNullBranchOfTheConditional()
    {
        using TestDatabase db = Setup();
        List<H21eLjOwner> owners = Owners();
        List<H21eLjNote> notes = Notes();

        List<(int Id, string? V)> expected = (from o in owners
                join p in notes.Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        List<(int Id, string? V)> actual = (from o in db.Table<H21eLjOwner>()
                join p in db.Table<H21eLjNote>().Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanProjectedRowNotNullFlagIsFalse()
    {
        using TestDatabase db = Setup();
        List<H21eLjOwner> owners = Owners();
        List<H21eLjNote> notes = Notes();

        List<(int Id, bool F)> expected = (from o in owners
                join p in notes.Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, F = p != null })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        List<(int Id, bool F)> actual = (from o in db.Table<H21eLjOwner>()
                join p in db.Table<H21eLjNote>().Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, F = p != null })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanCteRowTakesTheNullBranchOfTheConditional()
    {
        using TestDatabase db = Setup();
        List<H21eLjOwner> owners = Owners();
        List<H21eLjNote> notes = Notes();

        List<(int Id, string? V)> expected = (from o in owners
                join p in notes.Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        SQLiteCte<H21eLjPair> cte = db.With(() =>
            db.Table<H21eLjNote>().Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label }));

        List<(int Id, string? V)> actual = (from o in db.Table<H21eLjOwner>()
                join p in cte on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereOnFlattenedProjectedRowDropsOrphans()
    {
        using TestDatabase db = Setup();
        List<H21eLjOwner> owners = Owners();
        List<H21eLjNote> notes = Notes();

        List<string> expected = owners
            .GroupJoin(
                notes.Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Where(p => p != null)
            .Select(p => p!.Label)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H21eLjOwner>()
            .GroupJoin(
                db.Table<H21eLjNote>().Select(n => new H21eLjPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Where(p => p != null)
            .Select(p => p!.Label)
            .AsEnumerable()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MappedEntityOrphanRowStillTakesTheNullBranch()
    {
        using TestDatabase db = Setup();
        List<H21eLjOwner> owners = Owners();
        List<H21eLjNote> notes = Notes();

        List<(int Id, string? V)> expected = (from o in owners
                join n in notes on o.Id equals n.OwnerId into g
                from n in g.DefaultIfEmpty()
                select new { o.Id, V = n == null ? "none" : n.Label })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        List<(int Id, string? V)> actual = (from o in db.Table<H21eLjOwner>()
                join n in db.Table<H21eLjNote>() on o.Id equals n.OwnerId into g
                from n in g.DefaultIfEmpty()
                select new { o.Id, V = n == null ? "none" : n.Label })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
