using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22hNullOwners")]
public class H22hNullOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H22hNullNotes")]
public class H22hNullNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Label { get; set; } = "";
}

public class H22hSparePair
{
    public int Ref { get; set; }

    public string Label { get; set; } = "";

    public int Spare { get; set; }
}

public class H22hComputedPair
{
    public int Ref { get; set; }

    public string Label { get; set; } = "";

    public int Doubled => Ref * 2;
}

public class OptionalRowNullCheckUnprojectedColumnTests
{
    [Fact]
    public void OrphanRowOfAPartlyAssignedProjectionTakesTheNullBranch()
    {
        using TestDatabase db = Setup(nameof(OrphanRowOfAPartlyAssignedProjectionTakesTheNullBranch));
        List<H22hNullOwner> owners = Owners();
        List<H22hNullNote> notes = Notes();

        List<(int Id, string? V)> expected = (from o in owners
                join p in notes.Select(n => new H22hSparePair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        List<(int Id, string? V)> actual = (from o in db.Table<H22hNullOwner>()
                join p in db.Table<H22hNullNote>().Select(n => new H22hSparePair { Ref = n.OwnerId, Label = n.Label })
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
    public void OrphanRowOfAProjectionWithAComputedMemberTakesTheNullBranch()
    {
        using TestDatabase db = Setup(nameof(OrphanRowOfAProjectionWithAComputedMemberTakesTheNullBranch));
        List<H22hNullOwner> owners = Owners();
        List<H22hNullNote> notes = Notes();

        List<(int Id, string? V)> expected = (from o in owners
                join p in notes.Select(n => new H22hComputedPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        List<(int Id, string? V)> actual = (from o in db.Table<H22hNullOwner>()
                join p in db.Table<H22hNullNote>().Select(n => new H22hComputedPair { Ref = n.OwnerId, Label = n.Label })
                    on o.Id equals p.Ref into g
                from p in g.DefaultIfEmpty()
                select new { o.Id, V = p == null ? "none" : p.Label })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, (string?)t.V))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22hNullOwner> Owners()
    {
        return
        [
            new H22hNullOwner { Id = 1, Name = "Ann" },
            new H22hNullOwner { Id = 2, Name = "Bob" },
            new H22hNullOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H22hNullNote> Notes()
    {
        return
        [
            new H22hNullNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H22hNullNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22hNullOwner>().Schema.CreateTable();
        db.Table<H22hNullNote>().Schema.CreateTable();
        db.Table<H22hNullOwner>().AddRange(Owners());
        db.Table<H22hNullNote>().AddRange(Notes());
        return db;
    }
}
