using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bLooseOwners")]
public class H24bLooseOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24bLooseNotes")]
public class H24bLooseNote
{
    public int OwnerId { get; set; }

    public string? V { get; set; }
}

[Table("H24bTightNotes")]
public class H24bTightNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? V { get; set; }
}

public class OptionalRowWithoutKeyNullCheckTests
{
    [Fact]
    public void MatchedRowOfAKeylessTableWithANullColumnIsNotReportedAsMissing()
    {
        using TestDatabase db = Setup(nameof(MatchedRowOfAKeylessTableWithANullColumnIsNotReportedAsMissing));

        List<int> expected = Owners()
            .GroupJoin(LooseNotes(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { t.o.Id, N = n })
            .Where(x => x.N == null)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24bLooseOwner>()
            .GroupJoin(db.Table<H24bLooseNote>(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { t.o.Id, N = n })
            .Where(x => x.N == null)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MatchedRowOfAKeyedTableWithANullColumnIsNotReportedAsMissing()
    {
        using TestDatabase db = Setup(nameof(MatchedRowOfAKeyedTableWithANullColumnIsNotReportedAsMissing));

        List<int> expected = Owners()
            .GroupJoin(TightNotes(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { t.o.Id, N = n })
            .Where(x => x.N == null)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24bLooseOwner>()
            .GroupJoin(db.Table<H24bTightNote>(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => new { t.o.Id, N = n })
            .Where(x => x.N == null)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bLooseOwner> Owners()
    {
        return
        [
            new H24bLooseOwner { Id = 1, Name = "Ann" },
            new H24bLooseOwner { Id = 2, Name = "Bob" },
            new H24bLooseOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H24bLooseNote> LooseNotes()
    {
        return
        [
            new H24bLooseNote { OwnerId = 1, V = null },
            new H24bLooseNote { OwnerId = 3, V = "gamma" }
        ];
    }

    private static List<H24bTightNote> TightNotes()
    {
        return
        [
            new H24bTightNote { Id = 10, OwnerId = 1, V = null },
            new H24bTightNote { Id = 11, OwnerId = 3, V = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bLooseOwner>().Schema.CreateTable();
        db.Table<H24bLooseNote>().Schema.CreateTable();
        db.Table<H24bTightNote>().Schema.CreateTable();
        db.Table<H24bLooseOwner>().AddRange(Owners());
        db.Table<H24bLooseNote>().AddRange(LooseNotes());
        db.Table<H24bTightNote>().AddRange(TightNotes());
        return db;
    }
}
