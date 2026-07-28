using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23hOptionalOwners")]
public class H23hOptionalOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H23hOptionalNotes")]
public class H23hOptionalNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Label { get; set; } = "";
}

public class OptionalRowWholeEntitySelectedFromCarriedMemberTests
{
    [Fact]
    public void OrderedQuerySyntaxSelectingTheOptionalRowMarksMissingRowsNull()
    {
        using TestDatabase db = Setup(nameof(OrderedQuerySyntaxSelectingTheOptionalRowMarksMissingRowsNull));
        List<H23hOptionalOwner> owners = Owners();
        List<H23hOptionalNote> notes = Notes();

        List<int?> expected = (from o in owners
                join n in notes on o.Id equals n.OwnerId into g
                from n in g.DefaultIfEmpty()
                orderby o.Id
                select n)
            .Select(n => n == null ? (int?)null : n.Id)
            .ToList();

        List<int?> actual = (from o in db.Table<H23hOptionalOwner>()
                join n in db.Table<H23hOptionalNote>() on o.Id equals n.OwnerId into g
                from n in g.DefaultIfEmpty()
                orderby o.Id
                select n)
            .AsEnumerable()
            .Select(n => n == null ? (int?)null : n.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CarriedOptionalRowSelectedOnItsOwnMarksMissingRowsNull()
    {
        using TestDatabase db = Setup(nameof(CarriedOptionalRowSelectedOnItsOwnMarksMissingRowsNull));
        List<H23hOptionalOwner> owners = Owners();
        List<H23hOptionalNote> notes = Notes();

        List<int?> expected = owners
            .GroupJoin(notes, o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { Owner = t.o, Note = n })
            .OrderBy(x => x.Owner.Id)
            .Select(x => x.Note)
            .Select(n => n == null ? (int?)null : n.Id)
            .ToList();

        List<int?> actual = db.Table<H23hOptionalOwner>()
            .GroupJoin(db.Table<H23hOptionalNote>(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { Owner = t.o, Note = n })
            .OrderBy(x => x.Owner.Id)
            .Select(x => x.Note)
            .AsEnumerable()
            .Select(n => n == null ? (int?)null : n.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23hOptionalOwner> Owners()
    {
        return
        [
            new H23hOptionalOwner { Id = 1, Name = "Ann" },
            new H23hOptionalOwner { Id = 2, Name = "Bob" },
            new H23hOptionalOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H23hOptionalNote> Notes()
    {
        return
        [
            new H23hOptionalNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H23hOptionalNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23hOptionalOwner>().Schema.CreateTable();
        db.Table<H23hOptionalNote>().Schema.CreateTable();
        db.Table<H23hOptionalOwner>().AddRange(Owners());
        db.Table<H23hOptionalNote>().AddRange(Notes());
        return db;
    }
}
