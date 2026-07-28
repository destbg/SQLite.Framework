using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23gOwnerRows")]
public class H23gOwnerRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Extra { get; set; }
}

[Table("H23gOwnedRows")]
public class H23gOwnedRow
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }
}

public class CteInnerSelectMemberBesideArrayMemberTests
{
    [Fact]
    public void StringInsertMemberBesideArrayMemberKeepsItsValue()
    {
        using TestDatabase db = Setup();

        List<(int Id, string Marked)> expected = Owners()
            .Select(r => new { r.Id, Marked = r.Name.Insert(1, "-"), Tags = new[] { r.Extra } })
            .Select(x => (x.Id, x.Marked))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, string Marked)> actual = db.With(() => db.Table<H23gOwnerRow>()
                .Select(r => new { r.Id, Marked = r.Name.Insert(1, "-"), Tags = new[] { r.Extra } }))
            .Select(x => new { x.Id, x.Marked })
            .ToList()
            .Select(x => (x.Id, x.Marked))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CorrelatedCountMemberBesideArrayMemberKeepsItsValue()
    {
        using TestDatabase db = Setup();

        List<(int Id, int OwnedCount)> expected = Owners()
            .Select(r => new { r.Id, OwnedCount = OwnedRows().Count(c => c.OwnerId == r.Id), Tags = new[] { r.Extra } })
            .Select(x => (x.Id, x.OwnedCount))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, int OwnedCount)> actual = db.With(() => db.Table<H23gOwnerRow>()
                .Select(r => new
                {
                    r.Id,
                    OwnedCount = db.Table<H23gOwnedRow>().Count(c => c.OwnerId == r.Id),
                    Tags = new[] { r.Extra }
                }))
            .Select(x => new { x.Id, x.OwnedCount })
            .ToList()
            .Select(x => (x.Id, x.OwnedCount))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23gOwnerRow> Owners()
    {
        return
        [
            new H23gOwnerRow { Id = 1, Name = "alpha", Extra = 100 },
            new H23gOwnerRow { Id = 2, Name = "beta", Extra = 200 },
            new H23gOwnerRow { Id = 3, Name = "gamma", Extra = 300 }
        ];
    }

    private static List<H23gOwnedRow> OwnedRows()
    {
        return
        [
            new H23gOwnedRow { Id = 1, OwnerId = 1 },
            new H23gOwnedRow { Id = 2, OwnerId = 1 },
            new H23gOwnedRow { Id = 3, OwnerId = 2 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23gOwnerRow>().Schema.CreateTable();
        db.Table<H23gOwnedRow>().Schema.CreateTable();
        db.Table<H23gOwnerRow>().AddRange(Owners());
        db.Table<H23gOwnedRow>().AddRange(OwnedRows());
        return db;
    }
}
