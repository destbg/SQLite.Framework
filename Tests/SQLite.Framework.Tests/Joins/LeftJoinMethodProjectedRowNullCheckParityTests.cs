using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22mLjOwners")]
public class H22mLjOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H22mLjNotes")]
public class H22mLjNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Label { get; set; } = "";
}

public class H22mLjPair
{
    public int Ref { get; set; }

    public string Label { get; set; } = "";
}

public class LeftJoinMethodProjectedRowNullCheckParityTests
{
    [Fact]
    public void OrphanRowOfALeftJoinOverAProjectionReadsAsNull()
    {
        using TestDatabase db = Setup();

        List<(int Id, bool Missing)> expected = Owners()
            .GroupJoin(Pairs(), o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => (Id: t.o.Id, Missing: p == null))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, bool Missing)> actual = db.Table<H22mLjOwner>()
            .LeftJoin(
                db.Table<H22mLjNote>().Select(n => new H22mLjPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, p) => new { o.Id, Missing = p == null })
            .AsEnumerable()
            .Select(x => (Id: x.Id, Missing: x.Missing))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanRowOfALeftJoinOverAProjectionTakesTheNullBranch()
    {
        using TestDatabase db = Setup();

        List<(int Id, string? Value)> expected = Owners()
            .GroupJoin(Pairs(), o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => (Id: t.o.Id, Value: p == null ? "none" : p.Label))
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.Value))
            .ToList();

        List<(int Id, string? Value)> actual = db.Table<H22mLjOwner>()
            .LeftJoin(
                db.Table<H22mLjNote>().Select(n => new H22mLjPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, p) => new { o.Id, Value = p == null ? "none" : p.Label })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, (string?)x.Value))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanRowOfARightJoinOverAProjectionReadsAsNull()
    {
        using TestDatabase db = Setup();

        List<(int Id, bool Missing)> expected = Owners()
            .GroupJoin(Pairs(), o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => (Id: t.o.Id, Missing: p == null))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, bool Missing)> actual = db.Table<H22mLjNote>()
            .Select(n => new H22mLjPair { Ref = n.OwnerId, Label = n.Label })
            .RightJoin(
                db.Table<H22mLjOwner>(),
                p => p.Ref,
                o => o.Id,
                (p, o) => new { o!.Id, Missing = p == null })
            .AsEnumerable()
            .Select(x => (Id: x.Id, Missing: x.Missing))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanRowsOfAFullOuterJoinOverProjectionsReadAsNull()
    {
        using TestDatabase db = Setup();

        List<(int? Owner, int? Ref)> expected = Owners()
            .GroupJoin(Pairs(), o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => (Owner: (int?)t.o.Id, Ref: p == null ? null : (int?)p.Ref))
            .Concat(Pairs()
                .Where(p => Owners().All(o => o.Id != p.Ref))
                .Select(p => (Owner: (int?)null, Ref: (int?)p.Ref)))
            .OrderBy(t => t.Owner)
            .ThenBy(t => t.Ref)
            .ToList();

        List<(int? Owner, int? Ref)> actual = db.Table<H22mLjOwner>()
            .FullOuterJoin(
                db.Table<H22mLjNote>().Select(n => new H22mLjPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, p) => new { Owner = o == null ? null : (int?)o.Id, Ref = p == null ? null : (int?)p.Ref })
            .AsEnumerable()
            .Select(x => (x.Owner, x.Ref))
            .OrderBy(t => t.Owner)
            .ThenBy(t => t.Ref)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22mLjOwner> Owners()
    {
        return
        [
            new H22mLjOwner { Id = 1, Name = "Ann" },
            new H22mLjOwner { Id = 2, Name = "Bob" },
            new H22mLjOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H22mLjNote> Notes()
    {
        return
        [
            new H22mLjNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H22mLjNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static List<H22mLjPair> Pairs()
    {
        return Notes()
            .Select(n => new H22mLjPair { Ref = n.OwnerId, Label = n.Label })
            .ToList();
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22mLjOwner>().Schema.CreateTable();
        db.Table<H22mLjNote>().Schema.CreateTable();
        db.Table<H22mLjOwner>().AddRange(Owners());
        db.Table<H22mLjNote>().AddRange(Notes());
        return db;
    }
}
