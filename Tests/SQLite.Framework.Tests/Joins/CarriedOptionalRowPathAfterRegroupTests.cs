using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24qPathOwners")]
public class H24qPathOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24qPathNotes")]
public class H24qPathNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? V { get; set; }
}

public class H24qPathPair
{
    public string? V { get; set; }

    public int Ref { get; set; }
}

public class CarriedOptionalRowPathAfterRegroupTests
{
    [Fact]
    public void OptionalRowNullCheckInsideAGroupCountsOnlyMissingRows()
    {
        using TestDatabase db = Setup(nameof(OptionalRowNullCheckInsideAGroupCountsOnlyMissingRows));
        List<H24qPathOwner> owners = Owners();
        List<H24qPathPair> pairs = Pairs();

        List<int> expected = owners
            .GroupJoin(pairs, o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .GroupBy(x => x.Id)
            .Select(g => new { g.Key, Missing = g.Count(x => x.P == null) })
            .OrderBy(x => x.Key)
            .Select(x => x.Missing)
            .ToList();

        List<int> actual = db.Table<H24qPathOwner>()
            .GroupJoin(
                db.Table<H24qPathNote>().Select(n => new H24qPathPair { V = n.V, Ref = n.OwnerId }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .GroupBy(x => x.Id)
            .Select(g => new { g.Key, Missing = g.Count(x => x.P == null) })
            .AsEnumerable()
            .OrderBy(x => x.Key)
            .Select(x => x.Missing)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptionalRowNullCheckAfterTakeKeepsOnlyMissingRows()
    {
        using TestDatabase db = Setup(nameof(OptionalRowNullCheckAfterTakeKeepsOnlyMissingRows));
        List<H24qPathOwner> owners = Owners();
        List<H24qPathPair> pairs = Pairs();

        List<int> expected = owners
            .GroupJoin(pairs, o => o.Id, p => p.Ref, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Take(10)
            .Where(x => x.P == null)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24qPathOwner>()
            .GroupJoin(
                db.Table<H24qPathNote>().Select(n => new H24qPathPair { V = n.V, Ref = n.OwnerId }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => new { t.o.Id, P = p })
            .Take(10)
            .Where(x => x.P == null)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24qPathOwner> Owners()
    {
        return
        [
            new H24qPathOwner { Id = 1, Name = "Ann" },
            new H24qPathOwner { Id = 2, Name = "Bob" },
            new H24qPathOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H24qPathNote> Notes()
    {
        return
        [
            new H24qPathNote { Id = 10, OwnerId = 1, V = "alpha" },
            new H24qPathNote { Id = 11, OwnerId = 3, V = null }
        ];
    }

    private static List<H24qPathPair> Pairs()
    {
        return Notes()
            .Select(n => new H24qPathPair { V = n.V, Ref = n.OwnerId })
            .ToList();
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24qPathOwner>().Schema.CreateTable();
        db.Table<H24qPathNote>().Schema.CreateTable();
        db.Table<H24qPathOwner>().AddRange(Owners());
        db.Table<H24qPathNote>().AddRange(Notes());
        return db;
    }
}
