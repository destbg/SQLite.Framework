using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23hCarriedOwners")]
public class H23hCarriedOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H23hCarriedNotes")]
public class H23hCarriedNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? V { get; set; }
}

public class H23hCarriedPair
{
    public string? V { get; set; }

    public int Ref { get; set; }
}

public class CarriedOptionalRowNullCheckTests
{
    [Fact]
    public void MatchedRowWithANullColumnIsNotReportedAsMissing()
    {
        using TestDatabase db = Setup(nameof(MatchedRowWithANullColumnIsNotReportedAsMissing));
        List<H23hCarriedOwner> owners = Owners();
        List<H23hCarriedPair> pairs = Pairs();

        List<(int Id, bool Missing)> expected = owners
            .GroupJoin(pairs, o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Select(x => new { x.Id, Missing = x.P == null })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Missing))
            .ToList();

        List<(int Id, bool Missing)> actual = db.Table<H23hCarriedOwner>()
            .GroupJoin(
                db.Table<H23hCarriedNote>().Select(n => new H23hCarriedPair { V = n.V, Ref = n.OwnerId }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Select(x => new { x.Id, Missing = x.P == null })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Missing))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptionalRowCarriedInsideANestedMemberIsStillReportedAsMissing()
    {
        using TestDatabase db = Setup(nameof(OptionalRowCarriedInsideANestedMemberIsStillReportedAsMissing));
        List<H23hCarriedOwner> owners = Owners();
        List<H23hCarriedPair> pairs = Pairs();

        List<int> expected = owners
            .GroupJoin(pairs, o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Select(x => new { x.Id, W = new { x.P } })
            .Select(y => new { y.Id, Inner = y.W })
            .Where(z => z.Inner.P == null)
            .Select(z => z.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H23hCarriedOwner>()
            .GroupJoin(
                db.Table<H23hCarriedNote>().Select(n => new H23hCarriedPair { V = n.V, Ref = n.OwnerId }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Select(x => new { x.Id, W = new { x.P } })
            .Select(y => new { y.Id, Inner = y.W })
            .Where(z => z.Inner.P == null)
            .Select(z => z.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23hCarriedOwner> Owners()
    {
        return
        [
            new H23hCarriedOwner { Id = 1, Name = "Ann" },
            new H23hCarriedOwner { Id = 2, Name = "Bob" },
            new H23hCarriedOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H23hCarriedNote> Notes()
    {
        return
        [
            new H23hCarriedNote { Id = 10, OwnerId = 1, V = "alpha" },
            new H23hCarriedNote { Id = 11, OwnerId = 3, V = null }
        ];
    }

    private static List<H23hCarriedPair> Pairs()
    {
        return Notes()
            .Select(n => new H23hCarriedPair { V = n.V, Ref = n.OwnerId })
            .ToList();
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23hCarriedOwner>().Schema.CreateTable();
        db.Table<H23hCarriedNote>().Schema.CreateTable();
        db.Table<H23hCarriedOwner>().AddRange(Owners());
        db.Table<H23hCarriedNote>().AddRange(Notes());
        return db;
    }
}
