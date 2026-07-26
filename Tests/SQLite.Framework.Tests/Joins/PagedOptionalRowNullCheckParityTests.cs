using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22qPagedOwners")]
public class H22qPagedOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H22qPagedNotes")]
public class H22qPagedNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Label { get; set; } = "";
}

public class H22qPagedPair
{
    public int Ref { get; set; }

    public string Label { get; set; } = "";
}

public class PagedOptionalRowNullCheckParityTests
{
    [Fact]
    public void OrphanRowIsKeptByTheNullFilterAfterTake()
    {
        using TestDatabase db = Setup();

        List<string> expected = Owners()
            .GroupJoin(
                Notes().Select(n => new H22qPagedPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Take(10)
            .Where(p => p == null)
            .Select(p => p == null ? "orphan" : p.Label)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22qPagedOwner>()
            .GroupJoin(
                db.Table<H22qPagedNote>().Select(n => new H22qPagedPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Take(10)
            .Where(p => p == null)
            .AsEnumerable()
            .Select(p => p == null ? "orphan" : p.Label)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrphanRowIsDroppedByTheNotNullFilterAfterTake()
    {
        using TestDatabase db = Setup();

        List<string> expected = Owners()
            .GroupJoin(
                Notes().Select(n => new H22qPagedPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Take(10)
            .Where(p => p != null)
            .Select(p => p == null ? "orphan" : p.Label)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22qPagedOwner>()
            .GroupJoin(
                db.Table<H22qPagedNote>().Select(n => new H22qPagedPair { Ref = n.OwnerId, Label = n.Label }),
                o => o.Id,
                p => p.Ref,
                (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, p) => p)
            .Take(10)
            .Where(p => p != null)
            .AsEnumerable()
            .Select(p => p == null ? "orphan" : p.Label)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22qPagedOwner> Owners()
    {
        return
        [
            new H22qPagedOwner { Id = 1, Name = "Ann" },
            new H22qPagedOwner { Id = 2, Name = "Bob" },
            new H22qPagedOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H22qPagedNote> Notes()
    {
        return
        [
            new H22qPagedNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H22qPagedNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22qPagedOwner>().Schema.CreateTable();
        db.Table<H22qPagedNote>().Schema.CreateTable();
        db.Table<H22qPagedOwner>().AddRange(Owners());
        db.Table<H22qPagedNote>().AddRange(Notes());
        return db;
    }
}
